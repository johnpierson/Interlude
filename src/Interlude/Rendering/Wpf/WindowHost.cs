using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Autodesk.DesignScript.Runtime;
using Interlude.Runtime;

namespace Interlude.Rendering.Wpf;

/// <summary>
/// Gets a window onto the screen from wherever Dynamo happens to be executing.
///
/// There are three hosts to satisfy and they behave differently:
///
/// * Revit runs Dynamo's scheduler on Revit's own UI thread, so a form can be shown directly.
/// * Dynamo Sandbox runs the scheduler on a background thread, so the call has to be marshalled
///   onto the application dispatcher — which blocks the graph while the dialog pumps, exactly as
///   intended for a modal question.
/// * A command-line or scheduled run has no dispatcher at all, and the honest answer there is an
///   explanatory exception rather than a hang.
///
/// What this class never does is create its own STA thread when a host dispatcher exists. A
/// second UI thread inside Revit means a dialog the host cannot own, cannot order correctly, and
/// cannot reliably close.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal static class WindowHost
{
    /// <summary>
    /// Shows a window built by <paramref name="build"/> on the right thread and returns its result.
    /// </summary>
    /// <exception cref="HeadlessFormException">There is no user interface in this process.</exception>
    public static TResult ShowModal<TResult>(Func<Window, TResult> show, Func<Window> build, string formTitle)
    {
        if (build is null)
        {
            throw new ArgumentNullException(nameof(build));
        }

        Dispatcher? dispatcher = Application.Current?.Dispatcher;

        if (dispatcher is null)
        {
            throw new HeadlessFormException(formTitle, HostContext.Current);
        }

        if (dispatcher.CheckAccess())
        {
            return ShowOnUiThread(show, build);
        }

        // Blocking is the point: the graph is asking the user a question and cannot sensibly
        // continue until it is answered.
        return dispatcher.Invoke(() => ShowOnUiThread(show, build));
    }

    /// <summary>Whether a window can be shown in this process at all.</summary>
    public static bool CanShowWindows() => Application.Current?.Dispatcher is not null;

    private static TResult ShowOnUiThread<TResult>(Func<Window, TResult> show, Func<Window> build)
    {
        Window window = build();
        AttachToHost(window);
        return show(window);
    }

    /// <summary>
    /// Owns the dialog to the host's active window and centres it there.
    ///
    /// Ownership rather than <c>Topmost</c>: an owned window stays above Revit and moves with it
    /// without floating above every other application on the desktop. Centring is done by hand
    /// because <c>CenterOwner</c> only works for a WPF owner, and Revit's main window is Win32.
    /// </summary>
    private static void AttachToHost(Window window)
    {
        IntPtr owner = FindOwnerHandle();
        if (owner == IntPtr.Zero)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        new WindowInteropHelper(window) { Owner = owner };
        CenterOnHandle(window, owner);
    }

    private static IntPtr FindOwnerHandle()
    {
        try
        {
            IntPtr active = GetActiveWindow();
            if (active != IntPtr.Zero)
            {
                return active;
            }

            using Process current = Process.GetCurrentProcess();
            return current.MainWindowHandle;
        }
        catch (Exception ex) when (ex is InvalidOperationException or PlatformNotSupportedException)
        {
            return IntPtr.Zero;
        }
    }

    private static void CenterOnHandle(Window window, IntPtr owner)
    {
        if (!GetWindowRect(owner, out Rect bounds))
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        window.SourceInitialized += OnSourceInitialized;

        void OnSourceInitialized(object? sender, EventArgs e)
        {
            window.SourceInitialized -= OnSourceInitialized;

            PresentationSource? source = PresentationSource.FromVisual(window);
            double scaleX = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1d;
            double scaleY = source?.CompositionTarget?.TransformFromDevice.M22 ?? 1d;

            double ownerLeft = bounds.Left * scaleX;
            double ownerTop = bounds.Top * scaleY;
            double ownerWidth = (bounds.Right - bounds.Left) * scaleX;
            double ownerHeight = (bounds.Bottom - bounds.Top) * scaleY;

            // Measure first: with SizeToContent the height is still zero at this point.
            window.UpdateLayout();
            double width = double.IsNaN(window.Width) ? window.ActualWidth : window.Width;
            double height = window.ActualHeight > 0 ? window.ActualHeight : window.MinHeight;

            window.Left = ownerLeft + ((ownerWidth - width) / 2d);
            window.Top = ownerTop + ((ownerHeight - height) / 2.4d);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr handle, out Rect rectangle);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }
}
