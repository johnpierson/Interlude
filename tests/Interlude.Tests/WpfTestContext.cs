using System.Windows;

namespace Interlude.Tests;

/// <summary>
/// WPF needs an <see cref="Application"/> before <c>pack://application:,,,/</c> URIs resolve,
/// and the themes are loaded that way. Test hosts do not create one, so the STA tests do.
/// </summary>
internal static class WpfTestContext
{
    private static readonly object Gate = new();

    /// <summary>Ensures an application exists on this thread. Safe to call repeatedly.</summary>
    internal static void EnsureApplication()
    {
        lock (Gate)
        {
            if (Application.Current is null)
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
        }
    }
}
