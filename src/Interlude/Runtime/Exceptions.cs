using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Runtime;

/// <summary>Base type for the errors Interlude raises on purpose.</summary>
[IsVisibleInDynamoLibrary(false)]
public class InterludeException : InvalidOperationException
{
    public InterludeException(string message)
        : base(message)
    {
    }

    public InterludeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when computed values feed each other in a loop, for example a total that depends on
/// a subtotal that depends on the total.
///
/// This is detected while the session is being built, before a window is created, because the
/// alternative is a dialog that opens and then spins forever on the UI thread.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed class FormCycleException : InterludeException
{
    public FormCycleException(IReadOnlyList<string> cycle)
        : base(BuildMessage(cycle))
    {
        Cycle = cycle;
    }

    /// <summary>The keys involved, in the order they depend on each other.</summary>
    public IReadOnlyList<string> Cycle { get; }

    private static string BuildMessage(IReadOnlyList<string> cycle)
    {
        string path = cycle.Count > 0
            ? string.Join(" -> ", cycle.Concat(new[] { cycle[0] }))
            : "(unknown)";

        return "This form's computed values depend on each other in a loop, so no value could " +
               "ever settle: " + path + ". Break the loop by making one of these fields a plain input.";
    }
}

/// <summary>
/// Thrown when a form is asked to appear with no user interface available: a command-line
/// Dynamo run, a scheduled job, or an automated test.
///
/// Set <c>headlessUseDefaults</c> to true on <c>Form.Show</c> to return every field's default
/// instead of throwing.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed class HeadlessFormException : InterludeException
{
    public HeadlessFormException(string formTitle, HostContext host)
        : base($"The form '{formTitle}' cannot be shown because this Dynamo session has no user " +
               $"interface (host process '{host.ProcessName}'). Set headlessUseDefaults to true on " +
               "Form.Show to return each field's default value instead of stopping the graph.")
    {
        Host = host;
    }

    /// <summary>What was detected about the host process.</summary>
    public HostContext Host { get; }
}
