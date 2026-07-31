using System;
using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Model;

/// <summary>
/// Walks and rebuilds element trees.
///
/// Because elements are records, <c>with</c> produces a new instance of the same concrete type
/// without anyone having to write a visitor per element. That is what lets containers be
/// rewritten generically here, and what lets the <c>Behavior</c> nodes return a modified copy
/// of any element they are handed.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public static class ElementTree
{
    /// <summary>Every element beneath (and including) each root, depth first, parents first.</summary>
    public static IEnumerable<FormElement> Descend(IEnumerable<FormElement>? roots)
    {
        if (roots is null)
        {
            yield break;
        }

        foreach (FormElement root in roots)
        {
            foreach (FormElement element in Descend(root))
            {
                yield return element;
            }
        }
    }

    /// <summary>The element and everything beneath it, depth first, parents first.</summary>
    public static IEnumerable<FormElement> Descend(FormElement? element)
    {
        if (element is null)
        {
            yield break;
        }

        yield return element;

        if (element is ContainerElement container)
        {
            foreach (FormElement child in Descend(container.Children))
            {
                yield return child;
            }
        }
    }

    /// <summary>
    /// Rebuilds the tree, applying <paramref name="transform"/> to every element. The transform
    /// runs on a parent before its children, so a parent can be replaced wholesale.
    /// </summary>
    public static FormElement Rewrite(FormElement element, Func<FormElement, FormElement> transform)
    {
        if (element is null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        if (transform is null)
        {
            throw new ArgumentNullException(nameof(transform));
        }

        FormElement transformed = transform(element);

        if (transformed is not ContainerElement container || container.Children.Count == 0)
        {
            return transformed;
        }

        FormElement[] rewritten = new FormElement[container.Children.Count];
        bool changed = false;

        for (int i = 0; i < container.Children.Count; i++)
        {
            rewritten[i] = Rewrite(container.Children[i], transform);
            changed |= !ReferenceEquals(rewritten[i], container.Children[i]);
        }

        // Allocating a new container only when a child actually changed keeps record equality
        // meaningful: an untouched subtree stays reference-identical.
        return changed ? container with { Children = rewritten } : container;
    }

    /// <summary>Rebuilds a list of roots, applying <paramref name="transform"/> to every element.</summary>
    public static IReadOnlyList<FormElement> Rewrite(
        IReadOnlyList<FormElement> elements,
        Func<FormElement, FormElement> transform)
    {
        if (elements is null)
        {
            throw new ArgumentNullException(nameof(elements));
        }

        FormElement[] rewritten = new FormElement[elements.Count];
        for (int i = 0; i < elements.Count; i++)
        {
            rewritten[i] = Rewrite(elements[i], transform);
        }

        return rewritten;
    }

    /// <summary>
    /// Flattens whatever a Dynamo port handed us into a list of elements. Graph authors pass
    /// single elements, lists, and nested lists more or less interchangeably, and a form that
    /// silently drops half its controls because of a list-nesting mistake is a bad afternoon.
    /// </summary>
    public static IReadOnlyList<FormElement> Flatten(object? value)
    {
        List<FormElement> elements = new();
        FlattenInto(value, elements);
        return elements;
    }

    private static void FlattenInto(object? value, List<FormElement> target)
    {
        switch (value)
        {
            case null:
                return;
            case FormElement element:
                target.Add(element);
                return;
            case string:
                // A bare string on an elements port is a label, and reading it as a character
                // sequence would produce one control per letter.
                target.Add(new LabelElement { Text = (string)value });
                return;
            case System.Collections.IEnumerable enumerable:
                foreach (object? item in enumerable)
                {
                    FlattenInto(item, target);
                }

                return;
            default:
                // Anything else is shown rather than swallowed, so the mistake is visible.
                target.Add(new LabelElement { Text = Conditions.ValueOps.ToStringInvariant(value) });
                return;
        }
    }
}
