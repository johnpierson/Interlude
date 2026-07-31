using System;
using System.Collections.Concurrent;
using System.Threading;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Runtime;

/// <summary>
/// Stops one form from opening twice at once.
///
/// In Automatic run mode a graph can re-execute while its dialog is already on screen — an
/// upstream slider nudge, a background refresh — and the naive outcome is a stack of identical
/// modals the user has to dismiss one by one. Instead, the second caller waits for the first
/// window's answer and returns it, so a burst of executions produces one dialog and one result.
///
/// The wait has a timeout because the alternative failure mode is worse: a graph that never
/// finishes, with no dialog visible to explain why.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed class FormLatch
{
    /// <summary>Generous by design. This bounds a hang, it does not pace anything.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<string, Entry> _inFlight = new(StringComparer.Ordinal);

    /// <summary>The latch used by <c>Form.Show</c>.</summary>
    public static FormLatch Shared { get; } = new();

    /// <summary>Whether a form is on screen right now.</summary>
    public bool IsShowing(string formId)
        => !string.IsNullOrEmpty(formId) && _inFlight.ContainsKey(formId);

    /// <summary>
    /// Runs <paramref name="show"/> if this form is not already open; otherwise waits for the
    /// open one and returns its result. <paramref name="fallback"/> supplies a result when the
    /// wait times out or the owning call failed.
    /// </summary>
    public FormResult Run(
        string formId,
        Func<FormResult> show,
        Func<FormResult> fallback,
        TimeSpan? timeout = null)
    {
        if (show is null)
        {
            throw new ArgumentNullException(nameof(show));
        }

        if (fallback is null)
        {
            throw new ArgumentNullException(nameof(fallback));
        }

        if (string.IsNullOrEmpty(formId))
        {
            return show();
        }

        Entry candidate = new();
        Entry owner = _inFlight.GetOrAdd(formId, candidate);

        if (!ReferenceEquals(owner, candidate))
        {
            candidate.Dispose();
            return WaitFor(owner, fallback, timeout ?? DefaultTimeout);
        }

        try
        {
            candidate.Result = show();
            return candidate.Result;
        }
        finally
        {
            // Remove before signalling, so a waiter that wakes up and immediately re-runs the
            // same form is not told it is still in flight.
            _inFlight.TryRemove(formId, out _);
            candidate.Completed.Set();
        }
    }

    /// <summary>Forgets any in-flight entries. Intended for tests and for graph teardown.</summary>
    public void Reset()
    {
        foreach (string key in _inFlight.Keys)
        {
            if (_inFlight.TryRemove(key, out Entry? entry))
            {
                entry.Completed.Set();
                entry.Dispose();
            }
        }
    }

    private static FormResult WaitFor(Entry owner, Func<FormResult> fallback, TimeSpan timeout)
    {
        try
        {
            if (!owner.Completed.Wait(timeout))
            {
                return fallback();
            }
        }
        catch (ObjectDisposedException)
        {
            // The owner finished and tidied up between our lookup and our wait, which is the
            // race this catch exists for. Falling through re-reads its result.
        }

        return owner.Result ?? fallback();
    }

    private sealed class Entry : IDisposable
    {
        internal ManualResetEventSlim Completed { get; } = new(false);

        internal FormResult? Result { get; set; }

        public void Dispose() => Completed.Dispose();
    }
}
