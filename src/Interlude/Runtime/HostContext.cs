using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Runtime;

/// <summary>
/// What Interlude has been able to work out about the process it is running inside.
///
/// This type is deliberately free of WPF: it takes the facts it needs as constructor arguments,
/// which keeps the whole runtime layer testable and lets the WPF layer supply the one fact only
/// it can know — whether a dispatcher exists. The final say on whether a window can be shown
/// belongs to <c>WindowHost</c>, which checks for a live <c>Application</c>; the process-name
/// list here exists to make the resulting error message useful rather than to gate anything.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class HostContext
{
    /// <summary>
    /// Processes known to run Dynamo graphs with no user interface. Matched case-insensitively
    /// as a prefix, so "DynamoWPFCLI" also covers a versioned variant.
    /// </summary>
    private static readonly string[] HeadlessProcessNames =
    {
        "DynamoCLI",
        "DynamoWPFCLI",
        "DynamoPlayerCLI",
        "GenerativeDesign",
        "RevitBatchProcessor",
        "testhost",
        "vstest.console",
        "dotnet",
    };

    private static HostContext? _current;

    public HostContext(string processName, bool isUserInteractive)
    {
        ProcessName = processName ?? string.Empty;
        IsUserInteractive = isUserInteractive;
    }

    /// <summary>The real host, read once per process.</summary>
    public static HostContext Current => _current ??= Detect();

    /// <summary>The executable Interlude is loaded into, without its extension.</summary>
    public string ProcessName { get; }

    /// <summary>Whether the process has a desktop session at all.</summary>
    public bool IsUserInteractive { get; }

    /// <summary>True when the process name is one that never shows a window.</summary>
    public bool IsKnownHeadlessProcess => HeadlessProcessNames.Any(
        name => ProcessName.StartsWith(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// A best guess, before the WPF layer has had its say. A process with no desktop session
    /// certainly cannot show a form; a known command-line host almost certainly will not.
    /// </summary>
    public bool LooksHeadless => !IsUserInteractive || IsKnownHeadlessProcess;

    /// <summary>Builds a context with supplied facts, for tests.</summary>
    public static HostContext Create(string processName, bool isUserInteractive)
        => new(processName, isUserInteractive);

    /// <summary>Replaces the detected host. Intended for tests; pass null to go back to detection.</summary>
    public static void OverrideCurrent(HostContext? host) => _current = host;

    public override string ToString()
        => $"{ProcessName} (interactive: {IsUserInteractive}, known headless: {IsKnownHeadlessProcess})";

    private static HostContext Detect()
    {
        string processName;
        try
        {
            processName = Process.GetCurrentProcess().ProcessName;
        }
        catch (InvalidOperationException)
        {
            processName = "unknown";
        }

        return new HostContext(processName, Environment.UserInteractive);
    }
}
