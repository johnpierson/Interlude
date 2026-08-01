using System;
using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Runtime;

/// <summary>
/// Orders keys so that anything a value depends on is settled before the value itself.
///
/// Only computed values need this: they are the one place where a form's state feeds back into
/// itself. Visibility, enablement and validation read state but never write it, so they can be
/// evaluated in any order once values have settled.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class DependencyGraph
{
    private readonly Dictionary<string, HashSet<string>> _dependencies = new(StringComparer.Ordinal);

    /// <summary>Records that <paramref name="key"/> cannot be evaluated until <paramref name="dependency"/> is.</summary>
    public void AddDependency(string key, string dependency)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(dependency) ||
            string.Equals(key, dependency, StringComparison.Ordinal))
        {
            return;
        }

        Require(key).Add(dependency);
        Require(dependency);
    }

    /// <summary>Records a key that participates in ordering even if nothing depends on it.</summary>
    public void AddNode(string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            Require(key);
        }
    }

    /// <summary>What <paramref name="key"/> directly depends on.</summary>
    public IReadOnlyCollection<string> DependenciesOf(string key)
        => _dependencies.TryGetValue(key, out HashSet<string>? deps)
            ? deps
            : (IReadOnlyCollection<string>)Array.Empty<string>();

    /// <summary>
    /// Produces an evaluation order, or the cycle that makes one impossible.
    ///
    /// Uses an explicit stack rather than recursion: a pathological form should report a cycle,
    /// not overflow the UI thread's stack.
    /// </summary>
    public bool TrySort(out IReadOnlyList<string> ordered, out IReadOnlyList<string> cycle)
    {
        // 0 means unvisited; absent keys read as 0 from TryGetValue, which is the point.
        const int InProgress = 1;
        const int Done = 2;

        Dictionary<string, int> marks = new(StringComparer.Ordinal);
        List<string> result = new(_dependencies.Count);
        List<string> path = new();

        foreach (string root in _dependencies.Keys)
        {
            if (marks.TryGetValue(root, out int rootMark) && rootMark == Done)
            {
                continue;
            }

            Stack<Frame> stack = new();
            stack.Push(new Frame(root, GetEnumerator(root)));
            marks[root] = InProgress;
            path.Add(root);

            while (stack.Count > 0)
            {
                Frame frame = stack.Peek();

                if (frame.Pending.MoveNext())
                {
                    string next = frame.Pending.Current;
                    marks.TryGetValue(next, out int mark);

                    if (mark == Done)
                    {
                        continue;
                    }

                    if (mark == InProgress)
                    {
                        // `next` is already on the current path, so the walk from there to here
                        // and back is the loop the caller needs to be told about.
                        int start = path.LastIndexOf(next);
                        cycle = path.GetRange(start, path.Count - start);
                        ordered = Array.Empty<string>();
                        return false;
                    }

                    marks[next] = InProgress;
                    path.Add(next);
                    stack.Push(new Frame(next, GetEnumerator(next)));
                    continue;
                }

                stack.Pop();
                marks[frame.Key] = Done;
                result.Add(frame.Key);
                path.RemoveAt(path.Count - 1);
            }
        }

        ordered = result;
        cycle = Array.Empty<string>();
        return true;
    }

    private IEnumerator<string> GetEnumerator(string key)
        => _dependencies.TryGetValue(key, out HashSet<string>? deps)
            ? deps.GetEnumerator()
            : (IEnumerator<string>)((IEnumerable<string>)Array.Empty<string>()).GetEnumerator();

    private HashSet<string> Require(string key)
    {
        if (!_dependencies.TryGetValue(key, out HashSet<string>? deps))
        {
            deps = new HashSet<string>(StringComparer.Ordinal);
            _dependencies[key] = deps;
        }

        return deps;
    }

    private readonly struct Frame
    {
        internal Frame(string key, IEnumerator<string> pending)
        {
            Key = key;
            Pending = pending;
        }

        internal string Key { get; }

        internal IEnumerator<string> Pending { get; }
    }
}
