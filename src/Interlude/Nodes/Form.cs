using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;
using Interlude.Rendering;
using Interlude.Rendering.Wpf;
using Interlude.Runtime;
using Interlude.Serialization;
using Interlude.Theming;

namespace Interlude;

/// <summary>
/// Showing a form and getting the answers back.
///
/// A note on re-execution, because it surprises everyone once: Dynamo re-runs a graph whenever
/// anything upstream changes, and a node that shows a dialog will show it again. Interlude does
/// not pretend otherwise — it gives you the tools to control it. The <c>trigger</c> port skips the
/// dialog and returns the last answers when it is false, so a form can be gated behind a button
/// or a boolean. A form already on screen is never opened twice: a second execution waits for the
/// first window and returns its result rather than stacking dialogs. And Manual run mode remains
/// the right setting for any graph built around a form.
/// </summary>
public class Form
{
    private Form()
    {
    }

    /// <summary>
    /// Shows a form and waits for the user to answer it.
    ///
    /// Cancelling returns every field's default value rather than nulls, with
    /// <c>wasSubmitted</c> false. Check <c>wasSubmitted</c> before acting on the answers; you
    /// never need to null-check the values themselves.
    /// </summary>
    /// <param name="title">Shown in the window's title bar.</param>
    /// <param name="elements">The form's contents, built with the Input and Layout nodes.</param>
    /// <param name="trigger">
    /// Set to false to skip the dialog and return the last answers for this form. Anything else,
    /// including true, shows it. Doubles as a sequencing input.
    /// </param>
    /// <param name="submitText">Caption of the confirm button.</param>
    /// <param name="cancelText">Caption of the cancel button.</param>
    /// <param name="width">Window width in pixels.</param>
    /// <param name="maxHeight">Height at which the form starts scrolling.</param>
    /// <param name="formId">
    /// Identifies this form across runs, for remembered answers. Derived from the title and field
    /// keys when empty.
    /// </param>
    /// <param name="rememberValues">Pre-fill the form with the last answers it was submitted with.</param>
    /// <param name="headlessUseDefaults">
    /// What to do with no user interface, as in a command-line or scheduled run. False stops the
    /// graph with an explanation; true returns every field's default.
    /// </param>
    /// <param name="theme">Built with the Theme nodes. Null uses the system theme.</param>
    /// <param name="options">Built with Form.Options, for the less common settings.</param>
    /// <returns name="values">The answers, keyed by field.</returns>
    /// <returns name="wasSubmitted">True when the user confirmed rather than cancelled.</returns>
    /// <returns name="buttonClicked">Which button ended the form.</returns>
    /// <returns name="form">The full result, for the Result nodes.</returns>
    /// <search>form,show,dialog,ui,prompt,ask,input,data shapes</search>
    [MultiReturn(new[] { "values", "wasSubmitted", "buttonClicked", "form" })]
    public static Dictionary<string, object> Show(
        string title,
        List<object> elements,
        [DefaultArgument("true")] object trigger,
        string submitText = "Submit",
        string cancelText = "Cancel",
        double width = 420,
        double maxHeight = 800,
        string formId = "",
        bool rememberValues = true,
        bool headlessUseDefaults = false,
        [DefaultArgument("null")] object? theme = null,
        [DefaultArgument("null")] object? options = null)
    {
        FormDefinition definition = Create(
            title, elements, submitText, cancelText, width, maxHeight,
            formId, rememberValues, headlessUseDefaults, theme, options);

        return ShowDefinition(definition, trigger);
    }

    /// <summary>
    /// Shows a form that was built with <c>Form.Create</c> or loaded from JSON.
    ///
    /// The same dialog as <c>Form.Show</c>, and the same four outputs, differing only in where the
    /// form came from: a definition rather than a list of elements. This is the show half of the
    /// document workflow — <c>Form.FromJson</c> then this, and a graph runs a form maintained
    /// somewhere else entirely.
    ///
    /// Everything about re-execution applies here identically: the <c>trigger</c> port, the
    /// re-entrancy latch, and remembered answers.
    /// </summary>
    /// <param name="form">The form to show.</param>
    /// <param name="trigger">Set to false to skip the dialog and return the last answers.</param>
    /// <returns name="values">The answers, keyed by field.</returns>
    /// <returns name="wasSubmitted">True when the user confirmed rather than cancelled.</returns>
    /// <returns name="buttonClicked">Which button ended the form.</returns>
    /// <returns name="form">The full result, for the Result nodes.</returns>
    /// <search>form,show,definition,json,dialog</search>
    [MultiReturn(new[] { "values", "wasSubmitted", "buttonClicked", "form" })]
    public static Dictionary<string, object> ShowDefinition(
        FormDefinition form,
        [DefaultArgument("true")] object trigger)
    {
        if (form is null)
        {
            throw new ArgumentNullException(nameof(form), "There is no form to show.");
        }

        FormDefinition definition = form.WithResolvedKeys();
        string identity = definition.ResolveFormId();

        // Exactly false, not merely falsy: an unwired port must not silently skip the dialog.
        if (trigger is bool gate && !gate)
        {
            return Package(Skipped(definition, identity));
        }

        return Package(FormLatch.Shared.Run(
            identity,
            () => ShowOnce(definition, identity),
            () => FormResult.Cancelled(definition, FormButtonNames.Closed)));
    }

    /// <summary>
    /// Builds a form without showing it, for saving to JSON or for showing later.
    ///
    /// Splitting building from showing is what makes a form a document. Build it here, write it
    /// with <c>Form.ToJson</c>, and the definition can be reviewed in a pull request, diffed
    /// between releases and loaded by a graph that did not build it.
    ///
    /// It is also the way to check a form without a window appearing: feed the result to
    /// <c>Form.Check</c>. Nothing is drawn and nothing is remembered until it reaches
    /// <c>Form.ShowDefinition</c>.
    /// </summary>
    /// <param name="title">Shown in the window's title bar.</param>
    /// <param name="elements">The form's contents, built with the Input and Layout nodes.</param>
    /// <param name="submitText">Caption of the confirm button.</param>
    /// <param name="cancelText">Caption of the cancel button.</param>
    /// <param name="width">Window width in pixels.</param>
    /// <param name="maxHeight">Height at which the form starts scrolling.</param>
    /// <param name="formId">Identifies this form across runs.</param>
    /// <param name="rememberValues">Pre-fill with the last answers.</param>
    /// <param name="headlessUseDefaults">Return defaults instead of stopping when there is no UI.</param>
    /// <param name="theme">Built with the Theme nodes.</param>
    /// <param name="options">Built with Form.Options.</param>
    /// <returns name="form">The form definition.</returns>
    /// <search>form,create,build,definition,template</search>
    public static FormDefinition Create(
        string title,
        List<object> elements,
        string submitText = "Submit",
        string cancelText = "Cancel",
        double width = 420,
        double maxHeight = 800,
        string formId = "",
        bool rememberValues = true,
        bool headlessUseDefaults = false,
        [DefaultArgument("null")] object? theme = null,
        [DefaultArgument("null")] object? options = null)
    {
        FormDefinition definition = new()
        {
            Title = title ?? string.Empty,
            Elements = NodeSupport.FlattenElements(elements),
            Buttons = new FormButtons
            {
                SubmitText = string.IsNullOrWhiteSpace(submitText) ? "Submit" : submitText,
                CancelText = string.IsNullOrWhiteSpace(cancelText) ? "Cancel" : cancelText,
            },
            Window = new WindowOptions
            {
                Width = width <= 0 ? 420 : width,
                MaxHeight = maxHeight <= 0 ? 800 : maxHeight,
            },
            Theme = theme as ThemeDefinition ?? ThemeDefinition.Default,
            FormId = formId ?? string.Empty,
            RememberValues = rememberValues,
            HeadlessUseDefaults = headlessUseDefaults,
        };

        if (options is FormOptions extra)
        {
            definition = extra.ApplyTo(definition);
        }

        return definition.WithResolvedKeys();
    }

    /// <summary>
    /// The less common form settings, for <c>Form.Show</c>'s options port.
    ///
    /// <c>Form.Show</c> already carries the settings most forms need. This holds the rest, so that
    /// the node everyone uses does not have thirty ports on it — window behaviour, extra buttons,
    /// and whether a cancel button appears at all.
    /// </summary>
    /// <param name="description">A paragraph shown above the first field.</param>
    /// <param name="height">Fixed window height. Null sizes the window to its contents.</param>
    /// <param name="resizable">Let the user resize the window.</param>
    /// <param name="showCancel">Show the cancel button.</param>
    /// <param name="closeOnEscape">Let Escape cancel the form.</param>
    /// <param name="extraButtons">Extra footer buttons, built with Layout.Button.</param>
    /// <param name="iconPath">Path to a window icon.</param>
    /// <returns name="options">The options.</returns>
    /// <search>options,settings,description,height,resizable,buttons</search>
    public static FormOptions Options(
        string description = "",
        [DefaultArgument("null")] object? height = null,
        bool resizable = true,
        bool showCancel = true,
        bool closeOnEscape = true,
        [DefaultArgument("null")] List<object>? extraButtons = null,
        string iconPath = "")
        => new()
        {
            Description = NodeSupport.OrNull(description),
            Height = NodeSupport.OptionalDouble(height),
            IsResizable = resizable,
            ShowCancel = showCancel,
            CloseOnEscape = closeOnEscape,
            ExtraButtons = NodeSupport.Items(extraButtons).OfType<ButtonElement>().ToList(),
            IconPath = NodeSupport.OrNull(iconPath),
        };

    /// <summary>
    /// Writes a form to JSON, so it can be saved, reviewed and shared as a document.
    /// </summary>
    /// <param name="form">The form to write.</param>
    /// <param name="indented">Format for reading rather than for size.</param>
    /// <returns name="json">The form as JSON.</returns>
    /// <search>json,serialize,save,export,write</search>
    public static string ToJson(FormDefinition form, bool indented = true)
        => FormJson.Serialize(form, indented);

    /// <summary>
    /// Reads a form back from JSON, as written by <c>Form.ToJson</c>.
    ///
    /// This is what lets a graph show a form it did not build: the definition lives in a file
    /// under version control, and the graph loads and shows it. Change the form, and every graph
    /// that loads it changes with no graph edited.
    ///
    /// The schema version is checked before anything else is read, so a file written by a newer
    /// Interlude is refused with an explanation rather than half-understood. Feed the result to
    /// <c>Form.ShowDefinition</c>, and to <c>Form.Check</c> first if the file came from somewhere
    /// you do not control.
    /// </summary>
    /// <param name="json">The form as JSON.</param>
    /// <returns name="form">The form definition.</returns>
    /// <search>json,deserialize,load,import,read,parse</search>
    public static FormDefinition FromJson(string json) => FormJson.Deserialize(json);

    /// <summary>
    /// Replaces the options of one choice field in a form that already exists.
    ///
    /// This is what makes a form loaded from JSON usable with live model data. A checked-in form
    /// cannot carry Revit elements — they do not exist in another model, and saving them writes
    /// their names and says so — so the file holds the layout, the labels, the conditions and the
    /// validation, and the graph fills in the one field whose contents only the model knows:
    ///
    /// <code>Form.FromJson ──► Form.WithOptions(key: "levels", items: levels) ──► Form.ShowDefinition</code>
    ///
    /// The options behave exactly as they do on <c>Input.DropDown</c> and <c>Input.ListBox</c>,
    /// because they are the same options: the values go in whole and the selected one comes back
    /// as itself, not as its display name.
    ///
    /// Keys are resolved before the field is looked for, so a field that derives its key from its
    /// label can be named by that derived key — the same one the results come back under.
    ///
    /// Chain the node once per field. It returns a new form and changes nothing in place, so the
    /// definition loaded from the file is still there to be shown a second time.
    /// </summary>
    /// <param name="form">The form to fill in, usually straight from <c>Form.FromJson</c>.</param>
    /// <param name="key">Which field to fill in. Must be a drop-down, radio group or list box.</param>
    /// <param name="items">The values to choose between. Can be any objects.</param>
    /// <param name="displayNames">What to show for each item. Falls back to each item's own text.</param>
    /// <returns name="form">The form, with that field's options replaced.</returns>
    /// <search>options,items,fill,hydrate,revit,elements,json,dropdown,listbox</search>
    public static FormDefinition WithOptions(
        FormDefinition form,
        string key,
        [DefaultArgument("null")] List<object>? items = null,
        [DefaultArgument("null")] List<object>? displayNames = null)
    {
        if (form is null)
        {
            throw new ArgumentNullException(nameof(form), "There is no form to fill in.");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InterludeException("Form.WithOptions needs the key of the field to fill in.");
        }

        string wanted = key.Trim();
        FormDefinition resolved = form.WithResolvedKeys();
        IReadOnlyList<OptionItem> options = NodeSupport.Options(items, displayNames);

        FormElement? target = resolved
            .AllElements()
            .FirstOrDefault(element => string.Equals(element.Key, wanted, StringComparison.Ordinal));

        if (target is null)
        {
            throw new InterludeException(
                $"This form has no field called \"{wanted}\". " + DescribeChoiceFields(resolved));
        }

        if (target is not OptionInputElement and not ListSelectionElement)
        {
            throw new InterludeException(
                $"\"{wanted}\" is a {DescribeKind(target)}, which has no options to replace. " +
                DescribeChoiceFields(resolved));
        }

        return resolved with
        {
            Elements = ElementTree.Rewrite(
                resolved.Elements,
                element => ReferenceEquals(element, target) ? WithOptions(element, options) : element),
        };
    }

    /// <summary>
    /// Reports the problems Interlude can see in a form without showing it: conditions that name
    /// a field that does not exist, duplicate keys, and computed values that depend on each other
    /// in a loop.
    /// </summary>
    /// <param name="form">The form to check.</param>
    /// <returns name="isValid">True when nothing was found.</returns>
    /// <returns name="messages">What was found, if anything.</returns>
    /// <search>validate,check,lint,problems,warnings,debug</search>
    [MultiReturn(new[] { "isValid", "messages" })]
    public static Dictionary<string, object> Check(FormDefinition form)
    {
        if (form is null)
        {
            return new Dictionary<string, object>
            {
                ["isValid"] = false,
                ["messages"] = new List<string> { "There is no form to check." },
            };
        }

        try
        {
            FormSession session = new(form);

            return new Dictionary<string, object>
            {
                ["isValid"] = session.Warnings.Count == 0,
                ["messages"] = session.Warnings.ToList(),
            };
        }
        catch (InterludeException ex)
        {
            return new Dictionary<string, object>
            {
                ["isValid"] = false,
                ["messages"] = new List<string> { ex.Message },
            };
        }
    }

    /// <summary>
    /// Forgets the answers remembered for a form, so the next run starts from its defaults.
    /// </summary>
    /// <param name="formId">The form's id. Empty forgets every form.</param>
    /// <returns name="cleared">True once the answers have been forgotten.</returns>
    /// <search>forget,clear,reset,remember,cache</search>
    public static bool Forget(string formId = "")
    {
        if (string.IsNullOrWhiteSpace(formId))
        {
            SessionStore.Instance.Clear();
        }
        else
        {
            SessionStore.Instance.Remove(formId.Trim());
        }

        return true;
    }

    /// <summary>Puts a new set of options on whichever kind of choice input this is.</summary>
    private static FormElement WithOptions(FormElement element, IReadOnlyList<OptionItem> options)
    {
        FormElement replaced = element switch
        {
            OptionInputElement choice => choice with { Options = options },
            ListSelectionElement list => list with { Options = options },
            _ => element,
        };

        return replaced is InputElement input ? WithoutStaleDefault(input) : replaced;
    }

    /// <summary>
    /// Drops a default value that named an option the new list does not contain.
    ///
    /// The default in a checked-in form was written against the options that were in the file, and
    /// those are exactly what this node has just thrown away. Left in place it selects nothing, so
    /// the field is cleared back to the state it would have been in had the file never named a
    /// default — which for a drop-down or a radio group means its first option, the same as any
    /// choice field written without one. A drop-down with a placeholder is the exception: an
    /// author who wrote one asked for "nothing chosen yet" to be a state the form can be in.
    /// </summary>
    private static InputElement WithoutStaleDefault(InputElement input)
    {
        if (input.DefaultValue is null)
        {
            return input;
        }

        object? surviving = input.Coerce(input.DefaultValue);

        bool nothingLeft = surviving is null ||
            (surviving is System.Collections.IEnumerable items and not string &&
             !items.Cast<object?>().Any());

        if (!nothingLeft)
        {
            return input;
        }

        InputElement cleared = input with { DefaultValue = null };

        return cleared switch
        {
            DropdownElement dropdown when string.IsNullOrEmpty(dropdown.Placeholder)
                => dropdown with { SelectFirstByDefault = true },
            DropdownElement dropdown => dropdown,
            OptionInputElement choice => choice with { SelectFirstByDefault = true },
            _ => cleared,
        };
    }

    /// <summary>The element's kind, as a graph author would say it: "TextBox", "CheckBox".</summary>
    private static string DescribeKind(FormElement element)
    {
        string name = element.GetType().Name;

        return name.EndsWith("Element", StringComparison.Ordinal)
            ? name[..^"Element".Length]
            : name;
    }

    /// <summary>
    /// Names the fields that do have options, because the mistake behind both failures is almost
    /// always a key that is spelled differently in the file than in the graph.
    /// </summary>
    private static string DescribeChoiceFields(FormDefinition form)
    {
        string[] keys = form
            .AllElements()
            .Where(element => element is OptionInputElement or ListSelectionElement)
            .Select(element => element.Key)
            .Where(key => key.Length > 0)
            .ToArray();

        return keys.Length == 0
            ? "It has no drop-downs, radio groups or list boxes to fill in."
            : "Its choice fields are: " + string.Join(", ", keys) + ".";
    }

    /// <summary>
    /// Builds and shows the form once. Called under the latch, so at most one of these is running
    /// for a given form at a time.
    /// </summary>
    private static FormResult ShowOnce(FormDefinition definition, string identity)
    {
        IReadOnlyDictionary<string, object?>? remembered = definition.RememberValues
            ? SessionStore.Instance.TryGetValues(identity)
            : null;

        FormSession session = new(definition, remembered);

        if (!WindowHost.CanShowWindows())
        {
            // No dispatcher: a command-line run, a scheduled job, or a test host. Returning
            // defaults silently would let a graph appear to succeed having asked nobody
            // anything, so it has to be opted into.
            if (!definition.HeadlessUseDefaults)
            {
                throw new HeadlessFormException(definition.Title, HostContext.Current);
            }

            return session.BuildCancelledResult(FormButtonNames.Skipped);
        }

        IFormRenderer renderer = new WpfFormRenderer();
        FormResult result = renderer.ShowModal(definition, session);

        // Only a submitted result is remembered. Cancelling must not destroy the answers given
        // the last time the form was completed.
        SessionStore.Instance.Save(identity, result);
        return result;
    }

    /// <summary>The answer when the trigger gate is closed: last time's, or the defaults.</summary>
    private static FormResult Skipped(FormDefinition definition, string identity)
    {
        if (definition.RememberValues &&
            SessionStore.Instance.TryGet(identity, out FormResult? remembered) &&
            remembered is not null)
        {
            return remembered;
        }

        return FormResult.Cancelled(definition, FormButtonNames.Skipped);
    }

    private static Dictionary<string, object> Package(FormResult result)
        => new()
        {
            ["values"] = result.Values.ToDictionary(pair => pair.Key, pair => pair.Value!),
            ["wasSubmitted"] = result.WasSubmitted,
            ["buttonClicked"] = result.ButtonClicked,
            ["form"] = result,
        };
}
