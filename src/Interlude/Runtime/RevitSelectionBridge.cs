using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Runtime;

/// <summary>What a picking session came back with.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed record RevitSelectionOutcome
{
    /// <summary>The user pressed Escape or Cancel. Not a failure: the previous answer stands.</summary>
    public bool WasCancelled { get; init; }

    /// <summary>Why the pick could not run at all. Null when it ran.</summary>
    public string? Failure { get; init; }

    /// <summary>The picked elements, wrapped as Dynamo elements where possible.</summary>
    public IReadOnlyList<object> Elements { get; init; } = Array.Empty<object>();

    public static RevitSelectionOutcome Cancelled() => new() { WasCancelled = true };

    public static RevitSelectionOutcome Failed(string reason) => new() { Failure = reason };

    public static RevitSelectionOutcome Picked(IReadOnlyList<object> elements) => new() { Elements = elements };
}

/// <summary>
/// Lets a form ask the user to pick elements in the Revit model, without Interlude referencing a
/// single Revit assembly.
///
/// Everything here goes through reflection over assemblies that are already loaded when Dynamo
/// runs inside Revit — <c>RevitServices</c> for the active document, <c>RevitAPIUI</c> for the
/// picking calls, and <c>RevitNodes</c> to wrap the picked elements as the same
/// <c>Revit.Elements.Element</c> objects every Dynamo node downstream expects. That indirection
/// is the point, not an inconvenience: a compile-time Revit reference would multiply the build
/// matrix by every Revit version and end the zero-dependency rule, for three method calls.
///
/// Outside Revit — Sandbox, the preview harness, tests — the probe finds nothing and reports why,
/// and the control renders disabled instead of the package failing to load. Loading is the
/// difference that matters: reflection resolves names at call time, so Interlude.dll itself binds
/// against nothing Revit-shaped and imports cleanly everywhere.
///
/// Threading is inherited rather than managed here: the caller is a button handler inside a form
/// that <c>WindowHost</c> put on Revit's own UI thread, inside the API context Dynamo evaluates
/// in, which is exactly where <c>PickObjects</c> is allowed to run. Nothing in this class may be
/// called from anywhere else, which is why it is internal and the renderer is its only caller.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal static class RevitSelectionBridge
{
    /// <summary>
    /// Replaces the real picker, for tests. While set, the bridge also reports itself available,
    /// because a fake picker is only useful on a control whose button can be clicked.
    /// Null goes back to reflection.
    /// </summary>
    internal static Func<bool, string?, RevitSelectionOutcome>? OverridePicker { get; set; }

    // Resolved once and kept: the types and methods cannot change within a process, and probing
    // the whole AppDomain on every button click would be rude. The UIDocument itself is NOT
    // cached — the active document changes whenever the user switches models.
    private static MethodInfo? _getElement;
    private static MethodInfo? _toDsType;

    /// <summary>
    /// Why picking cannot work right now, or null when it can. Checked when the control is built,
    /// so a form shown in Sandbox gets a disabled button with an honest caption rather than a
    /// button that fails on click.
    /// </summary>
    public static string? UnavailableReason()
    {
        if (OverridePicker is not null)
        {
            return null;
        }

        return TryGetUIDocument(out _, out string? reason) ? null : reason;
    }

    /// <summary>
    /// Runs one picking session and returns what happened. Cancelling is reported as cancelled,
    /// not as a failure, because Escape means "keep what I had".
    /// </summary>
    public static RevitSelectionOutcome Pick(bool allowMultiple, string? prompt)
    {
        if (OverridePicker is not null)
        {
            return OverridePicker(allowMultiple, prompt);
        }

        if (!TryGetUIDocument(out object? uiDocument, out string? reason))
        {
            return RevitSelectionOutcome.Failed(reason!);
        }

        try
        {
            return PickCore(uiDocument!, allowMultiple, prompt);
        }
        catch (TargetInvocationException invocation) when (IsRevitCancellation(invocation.InnerException))
        {
            return RevitSelectionOutcome.Cancelled();
        }
        catch (Exception ex) when (ex is TargetInvocationException or InvalidOperationException
            or MemberAccessException or ArgumentException)
        {
            // A Revit API refusal (wrong context, view that cannot pick) must come back as words
            // on the form, never as an unhandled exception inside a message loop Revit owns.
            Exception reported = ex is TargetInvocationException { InnerException: not null } wrapped
                ? wrapped.InnerException!
                : ex;

            return RevitSelectionOutcome.Failed(reported.Message);
        }
    }

    /// <summary>
    /// A short human name for a picked element — the wrapper's own <c>Name</c> when it has one —
    /// for the summary line beside the button.
    /// </summary>
    public static string Describe(object? element)
    {
        if (element is null)
        {
            return string.Empty;
        }

        try
        {
            object? name = element.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(element);

            if (name is string text && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }
        catch (Exception ex) when (ex is TargetInvocationException or InvalidOperationException)
        {
            // An element whose Name throws (deleted since it was picked, say) still deserves a
            // label; ToString below always produces one.
        }

        return element.ToString() ?? string.Empty;
    }

    private static RevitSelectionOutcome PickCore(object uiDocument, bool allowMultiple, string? prompt)
    {
        object selection = GetProperty(uiDocument, "Selection")
            ?? throw new InvalidOperationException("The Revit selection interface could not be reached.");

        Type objectTypeEnum = selection.GetType().Assembly.GetType("Autodesk.Revit.UI.Selection.ObjectType", throwOnError: true)!;
        object elementKind = Enum.Parse(objectTypeEnum, "Element");

        string statusPrompt = string.IsNullOrWhiteSpace(prompt)
            ? (allowMultiple ? "Select elements, then press Finish." : "Select an element.")
            : prompt!;

        MethodInfo pick = selection.GetType().GetMethod(
                allowMultiple ? "PickObjects" : "PickObject",
                new[] { objectTypeEnum, typeof(string) })
            ?? throw new InvalidOperationException("This Revit version does not expose the expected picking method.");

        object? picked = pick.Invoke(selection, new[] { elementKind, statusPrompt });

        object document = GetProperty(uiDocument, "Document")
            ?? throw new InvalidOperationException("The active Revit document could not be reached.");

        List<object> elements = new();
        IEnumerable references = picked switch
        {
            null => Array.Empty<object>(),
            IEnumerable many when picked is not string => many,
            _ => new[] { picked },
        };

        foreach (object? reference in references)
        {
            if (reference is null)
            {
                continue;
            }

            object? element = GetElement(document, reference);
            if (element is not null)
            {
                elements.Add(WrapForDynamo(element));
            }
        }

        return RevitSelectionOutcome.Picked(elements);
    }

    private static object? GetElement(object document, object reference)
    {
        _getElement ??= document.GetType().GetMethod("GetElement", new[] { reference.GetType() });

        return _getElement is null
            ? null
            : _getElement.Invoke(document, new[] { reference });
    }

    /// <summary>
    /// Wraps a raw <c>Autodesk.Revit.DB.Element</c> as the <c>Revit.Elements.Element</c> the rest
    /// of a Dynamo graph works with, via <c>ElementWrapper.ToDSType</c>. <c>isRevitOwned: true</c>
    /// because the user picked something that exists in the model — Dynamo must never treat it as
    /// its own and delete it on re-run.
    /// </summary>
    private static object WrapForDynamo(object element)
    {
        if (_toDsType is null)
        {
            Assembly? revitNodes = FindAssembly("RevitNodes");
            Type? wrapper = revitNodes?.GetType("Revit.Elements.ElementWrapper");

            _toDsType = wrapper?
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                {
                    if (!string.Equals(method.Name, "ToDSType", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    return parameters.Length == 2
                        && parameters[1].ParameterType == typeof(bool)
                        && string.Equals(
                            parameters[0].ParameterType.FullName,
                            "Autodesk.Revit.DB.Element",
                            StringComparison.Ordinal);
                });
        }

        if (_toDsType is null)
        {
            // Better a raw element than nothing: Rhythm-style graphs can still use it, and the
            // summary line still describes it.
            return element;
        }

        try
        {
            return _toDsType.Invoke(null, new[] { element, true }) ?? element;
        }
        catch (TargetInvocationException)
        {
            return element;
        }
    }

    private static bool TryGetUIDocument(out object? uiDocument, out string? reason)
    {
        uiDocument = null;

        Assembly? services = FindAssembly("RevitServices");
        if (services is null)
        {
            // Short on purpose: this is the summary line of a control that may be sitting in a
            // narrow form, not a paragraph.
            reason = "Only available inside Revit.";
            return false;
        }

        Type? manager = services.GetType("RevitServices.Persistence.DocumentManager");
        object? instance = manager?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
            ?.GetValue(null);

        object? uiApplication = instance is null ? null : GetProperty(instance, "CurrentUIApplication");
        uiDocument = uiApplication is null ? null : GetProperty(uiApplication, "ActiveUIDocument");

        if (uiDocument is null)
        {
            reason = "No open Revit document.";
            return false;
        }

        reason = null;
        return true;
    }

    private static Assembly? FindAssembly(string name)
        => AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, name, StringComparison.Ordinal));

    private static object? GetProperty(object owner, string name)
        => owner.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(owner);

    private static bool IsRevitCancellation(Exception? exception)
        => exception is not null
            && exception.GetType().FullName == "Autodesk.Revit.Exceptions.OperationCanceledException";
}
