using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;
using Interlude.Model;
using Interlude.Validation;

namespace Interlude.Runtime;

/// <summary>
/// The live state of one form, and the reason Interlude has no imperative wiring.
///
/// A session is built once per showing. At construction it walks every VisibleIf, EnabledIf,
/// RequiredIf, computed value and validation rule, asks each what it depends on, and orders the
/// computed values so nothing is evaluated before its inputs. Cycles are found here, before a
/// window exists, because the alternative is a dialog that hangs.
///
/// From then on the contract is one-way and tiny: a control reports an edit through
/// <see cref="SetValue(string, object?)"/>, the session recomputes everything affected, and
/// raises exactly one <see cref="Changed"/> event carrying the whole batch. Controls never talk
/// to each other, and the renderer's entire job is to apply batches. That is also why the
/// interesting tests in this repository need no UI at all: feed values in, assert visibility,
/// errors and computed values out.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed class FormSession
{
    private readonly FormStateStore _store = new();
    private readonly Dictionary<FormElement, ElementRuntimeState> _states;
    private readonly Dictionary<FormElement, FormElement?> _parents;
    private readonly Dictionary<string, InputElement> _inputsByKey;
    private readonly IReadOnlyList<FormElement> _ordered;
    private readonly IReadOnlyList<string> _computedOrder;
    private readonly List<string> _warnings = new();

    private int _propagationDepth;
    private bool _showAllErrors;

    /// <summary>
    /// Builds a session for <paramref name="definition"/>, optionally pre-filled with values
    /// from a previous run.
    /// </summary>
    /// <exception cref="FormCycleException">Computed values depend on each other in a loop.</exception>
    public FormSession(FormDefinition definition, IReadOnlyDictionary<string, object?>? initialValues = null)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        // Idempotent, so callers that already resolved keys pay nothing and callers that
        // forgot still get a form whose results are addressable.
        Definition = definition.WithResolvedKeys();

        _ordered = Definition.AllElements().ToList();
        _states = new Dictionary<FormElement, ElementRuntimeState>(ReferenceComparer<FormElement>.Instance);
        _parents = new Dictionary<FormElement, FormElement?>(ReferenceComparer<FormElement>.Instance);
        _inputsByKey = new Dictionary<string, InputElement>(StringComparer.Ordinal);

        IndexTree();
        SeedValues(initialValues);

        _computedOrder = BuildComputedOrder();
        CollectUnknownKeyWarnings();

        // Settle the form before anyone can see it: computed values, visibility and validation
        // must all be correct on the very first frame, not after the first keystroke.
        Propagate(raiseEvent: false, seed: null);
    }

    /// <summary>Raised once per edit, carrying every state change that edit caused.</summary>
    internal event EventHandler<FormStateChangedEventArgs>? Changed;

    /// <summary>The form being shown, with keys resolved.</summary>
    public FormDefinition Definition { get; }

    /// <summary>Every element, depth first, parents before children.</summary>
    public IReadOnlyList<FormElement> Elements => _ordered;

    /// <summary>
    /// Authoring problems that are worth reporting but not worth refusing to show the form
    /// over, such as a condition referring to a key no field uses.
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <summary>
    /// When true, every failing field shows its message. Until then only fields the user has
    /// actually touched do, so a form does not open covered in red.
    ///
    /// Flipping this raises a <see cref="Changed"/> batch for every failing field. It has to:
    /// on a failed submit the errors themselves have not changed — an empty required field is
    /// still empty — so without this the renderer would be told nothing and the fields would
    /// stay silent while the submit button appeared to do nothing at all.
    /// </summary>
    public bool ShowAllErrors
    {
        get => _showAllErrors;
        set
        {
            if (_showAllErrors == value)
            {
                return;
            }

            _showAllErrors = value;
            RaiseErrorVisibilityChanged();
        }
    }

    /// <summary>True when no visible field is failing a rule.</summary>
    public bool IsValid => _states.Values.All(state => !state.IsVisible || state.IsValid);

    /// <summary>Current state for an element.</summary>
    internal ElementRuntimeState GetState(FormElement element)
        => _states.TryGetValue(element, out ElementRuntimeState? state)
            ? state
            : throw new ArgumentException("This element is not part of the form.", nameof(element));

    /// <summary>Current state for a field, or null when no field uses that key.</summary>
    internal ElementRuntimeState? GetState(string key)
        => _inputsByKey.TryGetValue(key ?? string.Empty, out InputElement? input) ? _states[input] : null;

    /// <summary>Current value of a field.</summary>
    public object? GetValue(string key) => _store.GetValue(key ?? string.Empty);

    /// <summary>Every field's current value.</summary>
    public IReadOnlyDictionary<string, object?> Values => _store.Snapshot();

    /// <summary>Every field's default value, which is what a cancelled form returns.</summary>
    public IReadOnlyDictionary<string, object?> Defaults
    {
        get
        {
            Dictionary<string, object?> defaults = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, InputElement> pair in _inputsByKey)
            {
                defaults[pair.Key] = pair.Value.GetEffectiveDefault();
            }

            return defaults;
        }
    }

    /// <summary>Messages for every field currently failing a rule, keyed by field.</summary>
    public IReadOnlyDictionary<string, string> Errors
    {
        get
        {
            Dictionary<string, string> errors = new(StringComparer.Ordinal);
            foreach (ElementRuntimeState state in _states.Values)
            {
                if (state.IsVisible && state.Error is not null && !string.IsNullOrEmpty(state.Key))
                {
                    errors[state.Key] = state.Error;
                }
            }

            return errors;
        }
    }

    /// <summary>
    /// Records an edit and propagates its consequences. Returns false when the value did not
    /// actually change, when the key is unknown, or when the field is computed and therefore
    /// not the user's to set.
    /// </summary>
    public bool SetValue(string key, object? value)
    {
        if (key is null || !_inputsByKey.TryGetValue(key, out InputElement? input))
        {
            return false;
        }

        if (input.Computed is not null)
        {
            // A computed field is driven by the form. Accepting a write here would produce a
            // value that the very next propagation pass silently overwrites.
            return false;
        }

        object? coerced = input.Coerce(value);
        if (!_store.Set(key, coerced))
        {
            return false;
        }

        ElementRuntimeState state = _states[input];
        state.Value = coerced;
        state.IsTouched = true;

        Propagate(raiseEvent: true, seed: (input, StateChangeKind.Value));
        return true;
    }

    /// <summary>Records several edits, propagating once for the whole set.</summary>
    public void SetValues(IReadOnlyDictionary<string, object?>? values, bool markTouched = false)
    {
        if (values is null || values.Count == 0)
        {
            return;
        }

        bool anyChanged = false;

        foreach (KeyValuePair<string, object?> pair in values)
        {
            if (!_inputsByKey.TryGetValue(pair.Key, out InputElement? input) || input.Computed is not null)
            {
                continue;
            }

            object? coerced = input.Coerce(pair.Value);
            if (!_store.Set(pair.Key, coerced))
            {
                continue;
            }

            ElementRuntimeState state = _states[input];
            state.Value = coerced;
            state.IsTouched |= markTouched;
            anyChanged = true;
        }

        if (anyChanged)
        {
            Propagate(raiseEvent: true, seed: null);
        }
    }

    /// <summary>Puts every field back to its default value.</summary>
    public void Reset()
    {
        foreach (KeyValuePair<string, InputElement> pair in _inputsByKey)
        {
            _store.Set(pair.Key, pair.Value.GetEffectiveDefault());
            ElementRuntimeState state = _states[pair.Value];
            state.Value = _store.GetValue(pair.Key);
            state.IsTouched = false;
        }

        ShowAllErrors = false;
        Propagate(raiseEvent: true, seed: null);
    }

    /// <summary>
    /// Re-runs validation and reveals every failing field. Returns true when the form may be
    /// submitted.
    /// </summary>
    public bool Validate()
    {
        // Recompute first, then reveal: the reveal batch should carry current messages, not the
        // ones from before this pass.
        Propagate(raiseEvent: true, seed: null);
        ShowAllErrors = true;
        return IsValid;
    }

    /// <summary>The first visible field currently failing a rule, for focusing on a failed submit.</summary>
    internal ElementRuntimeState? FirstInvalid()
    {
        foreach (FormElement element in _ordered)
        {
            ElementRuntimeState state = _states[element];
            if (state.IsVisible && !state.IsValid)
            {
                return state;
            }
        }

        return null;
    }

    /// <summary>
    /// Validates and, if the form is complete, produces the submitted result.
    /// Returns false and leaves <paramref name="result"/> null when validation fails.
    /// </summary>
    public bool TrySubmit(string buttonClicked, out FormResult? result)
    {
        if (!Validate())
        {
            result = null;
            return false;
        }

        result = BuildResult(wasSubmitted: true, buttonClicked);
        return true;
    }

    /// <summary>
    /// Builds the cancelled result: every field's default, never the values the user was part
    /// way through typing, and never nulls.
    /// </summary>
    public FormResult BuildCancelledResult(string buttonClicked = FormButtonNames.Cancel)
        => new(Defaults, false, buttonClicked, Definition);

    /// <summary>Builds a result from the current values.</summary>
    public FormResult BuildResult(bool wasSubmitted, string buttonClicked)
        => new(_store.Snapshot(), wasSubmitted, buttonClicked, Definition);

    private void IndexTree()
    {
        foreach (FormElement element in _ordered)
        {
            _states[element] = new ElementRuntimeState(element);
            _parents[element] = null;
        }

        foreach (FormElement element in _ordered)
        {
            if (element is ContainerElement container)
            {
                foreach (FormElement child in container.Children)
                {
                    _parents[child] = element;
                }
            }

            if (element is InputElement input && !string.IsNullOrEmpty(input.Key))
            {
                // Keys were de-duplicated by FormKeys.Assign, so a clash here would mean the
                // caller hand-built a tree; last one wins and the warning explains why.
                if (_inputsByKey.ContainsKey(input.Key))
                {
                    _warnings.Add($"More than one field uses the key '{input.Key}'. Only the last one will be readable.");
                }

                _inputsByKey[input.Key] = input;
            }
        }
    }

    private void SeedValues(IReadOnlyDictionary<string, object?>? initialValues)
    {
        foreach (KeyValuePair<string, InputElement> pair in _inputsByKey)
        {
            InputElement input = pair.Value;

            object? value = initialValues is not null &&
                            initialValues.TryGetValue(pair.Key, out object? remembered)
                ? input.Coerce(remembered)
                : input.GetEffectiveDefault();

            _store.Set(pair.Key, value);
            _states[input].Value = value;
        }
    }

    private IReadOnlyList<string> BuildComputedOrder()
    {
        DependencyGraph graph = new();

        foreach (KeyValuePair<string, InputElement> pair in _inputsByKey)
        {
            graph.AddNode(pair.Key);

            if (pair.Value.Computed is null)
            {
                continue;
            }

            foreach (string dependency in pair.Value.Computed.DependsOn())
            {
                graph.AddDependency(pair.Key, dependency);
            }
        }

        if (!graph.TrySort(out IReadOnlyList<string> ordered, out IReadOnlyList<string> cycle))
        {
            throw new FormCycleException(cycle);
        }

        // Only computed fields need evaluating; the rest are in the graph purely to be ordered
        // against.
        return ordered.Where(key => _inputsByKey.TryGetValue(key, out InputElement? input)
                                    && input.Computed is not null).ToList();
    }

    private void CollectUnknownKeyWarnings()
    {
        HashSet<string> referenced = new(StringComparer.Ordinal);

        foreach (FormElement element in _ordered)
        {
            foreach (string key in element.BehaviourDependencies())
            {
                referenced.Add(key);
            }

            if (element is InputElement { Computed: not null } input)
            {
                foreach (string key in input.Computed!.DependsOn())
                {
                    referenced.Add(key);
                }
            }
        }

        foreach (string key in referenced.OrderBy(k => k, StringComparer.Ordinal))
        {
            if (!_inputsByKey.ContainsKey(key))
            {
                _warnings.Add(
                    $"A condition or computed value refers to '{key}', but no field uses that key. " +
                    "It will always read as empty.");
            }
        }
    }

    /// <summary>
    /// Recomputes the whole form and raises at most one event.
    ///
    /// Every pass re-evaluates every computed value, condition and rule rather than tracking
    /// which ones a particular edit could have touched. Forms are tens of fields, not millions
    /// of cells, so the full pass costs nothing measurable and removes the entire class of bugs
    /// where an incremental update misses a dependency. The dependency graph still matters: it
    /// fixes the *order* computed values are evaluated in, and it is what catches cycles.
    /// </summary>
    private void Propagate(bool raiseEvent, (FormElement Element, StateChangeKind Kind)? seed)
    {
        Dictionary<FormElement, StateChangeKind> changes =
            new(ReferenceComparer<FormElement>.Instance);

        if (seed.HasValue)
        {
            changes[seed.Value.Element] = seed.Value.Kind;
        }

        // Guards against a renderer writing back into the session while it applies a batch.
        // The value still lands in the store; it simply does not start a nested pass.
        if (_propagationDepth > 0)
        {
            return;
        }

        _propagationDepth++;
        try
        {
            EvaluateComputedValues(changes);
            EvaluateVisibilityAndEnablement(changes);
            EvaluateValidation(changes);
        }
        finally
        {
            _propagationDepth--;
        }

        if (!raiseEvent || changes.Count == 0 || Changed is null)
        {
            return;
        }

        List<ElementStateChange> batch = new(changes.Count);
        foreach (FormElement element in _ordered)
        {
            if (changes.TryGetValue(element, out StateChangeKind kind) && kind != StateChangeKind.None)
            {
                batch.Add(new ElementStateChange(_states[element], kind));
            }
        }

        if (batch.Count > 0)
        {
            Changed?.Invoke(this, new FormStateChangedEventArgs(batch));
        }
    }

    private void EvaluateComputedValues(Dictionary<FormElement, StateChangeKind> changes)
    {
        foreach (string key in _computedOrder)
        {
            InputElement input = _inputsByKey[key];
            object? next = input.Coerce(input.Computed!.Compute(_store));

            if (_store.Set(key, next))
            {
                _states[input].Value = next;
                Mark(changes, input, StateChangeKind.Value);
            }
        }
    }

    private void EvaluateVisibilityAndEnablement(Dictionary<FormElement, StateChangeKind> changes)
    {
        // _ordered is pre-order, so a parent's resolved state is always available to its children.
        foreach (FormElement element in _ordered)
        {
            ElementRuntimeState state = _states[element];

            bool parentVisible = true;
            bool parentEnabled = true;

            if (_parents.TryGetValue(element, out FormElement? parent) && parent is not null)
            {
                ElementRuntimeState parentState = _states[parent];
                parentVisible = parentState.IsVisible;
                parentEnabled = parentState.IsEnabled;
            }

            bool visible = parentVisible && (element.VisibleIf?.Evaluate(_store) ?? true);
            bool enabled = parentEnabled && (element.EnabledIf?.Evaluate(_store) ?? true);
            bool required = visible && (element.RequiredIf?.Evaluate(_store) ?? false);

            if (state.IsVisible != visible)
            {
                state.IsVisible = visible;
                Mark(changes, element, StateChangeKind.Visibility);
            }

            if (state.IsEnabled != enabled)
            {
                state.IsEnabled = enabled;
                Mark(changes, element, StateChangeKind.Enabled);
            }

            if (state.IsRequired != required)
            {
                state.IsRequired = required;
                Mark(changes, element, StateChangeKind.Required);
            }
        }
    }

    private void EvaluateValidation(Dictionary<FormElement, StateChangeKind> changes)
    {
        foreach (FormElement element in _ordered)
        {
            ElementRuntimeState state = _states[element];
            string? error = state.IsVisible ? ValidateElement(element, state) : null;

            if (!string.Equals(state.Error, error, StringComparison.Ordinal))
            {
                state.Error = error;
                Mark(changes, element, StateChangeKind.Validation);
            }
        }
    }

    private string? ValidateElement(FormElement element, ElementRuntimeState state)
    {
        // A hidden field is not the user's problem, and a required rule on one would block
        // submission with no way to see why. EvaluateValidation already skipped those.
        if (state.IsRequired && ValueOps.IsEmpty(state.Value))
        {
            return string.IsNullOrWhiteSpace(element.RequiredMessage)
                ? "This field is required."
                : element.RequiredMessage;
        }

        foreach (ValidationRule rule in element.Rules)
        {
            ValidationOutcome outcome = rule.Validate(state.Value, _store);
            if (!outcome.IsValid)
            {
                return outcome.Message ?? "This value is not valid.";
            }
        }

        return null;
    }

    /// <summary>
    /// Tells the renderer to re-decide which error messages are visible. No state changed here,
    /// only the policy about what may be shown, so the batch is purely a validation refresh.
    /// </summary>
    private void RaiseErrorVisibilityChanged()
    {
        if (Changed is null)
        {
            return;
        }

        List<ElementStateChange> batch = new();

        foreach (FormElement element in _ordered)
        {
            ElementRuntimeState state = _states[element];
            if (state.IsVisible && state.Error is not null)
            {
                batch.Add(new ElementStateChange(state, StateChangeKind.Validation));
            }
        }

        if (batch.Count > 0)
        {
            Changed.Invoke(this, new FormStateChangedEventArgs(batch));
        }
    }

    private static void Mark(
        Dictionary<FormElement, StateChangeKind> changes,
        FormElement element,
        StateChangeKind kind)
    {
        changes[element] = changes.TryGetValue(element, out StateChangeKind existing)
            ? existing | kind
            : kind;
    }

    /// <summary>
    /// Identity comparison for elements. Records compare by value, and two sibling spacers with
    /// the same size are genuinely equal — but they are still two different controls on screen,
    /// so the session must key on identity.
    /// </summary>
    private sealed class ReferenceComparer<T> : IEqualityComparer<T>
        where T : class
    {
        internal static readonly ReferenceComparer<T> Instance = new();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
