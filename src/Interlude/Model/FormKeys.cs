using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Model;

/// <summary>
/// Turns labels into result keys.
///
/// This algorithm is a versioned API contract, not an implementation detail. A graph that reads
/// <c>values["wall_type"]</c> keeps working only for as long as "Wall Type" keeps slugifying to
/// <c>wall_type</c>, so changing the rules below means bumping <see cref="SlugVersion"/> and
/// treating it as a breaking change. Prefer giving inputs explicit keys in any graph you intend
/// to keep.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public static class FormKeys
{
    /// <summary>
    /// Version of the slug rules. Bumping this is a breaking change to every graph that relies
    /// on derived keys.
    /// </summary>
    public const int SlugVersion = 1;

    /// <summary>The key given to an element whose label yields nothing usable.</summary>
    public const string FallbackKey = "field";

    /// <summary>
    /// Derives a result key from a label.
    ///
    /// The rules, frozen at <see cref="SlugVersion"/> 1: strip accents, lowercase invariantly,
    /// replace every run of non-alphanumeric characters with a single underscore, and trim
    /// underscores from both ends. "Wall Type" becomes <c>wall_type</c>, "Höhe (mm)" becomes
    /// <c>hohe_mm</c>. Note that characters with no decomposition, such as the German sharp s,
    /// are not transliterated: "Straße" becomes <c>stra_e</c>.
    /// </summary>
    public static string Slugify(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return FallbackKey;
        }

        // FormD splits "é" into "e" + combining accent so the accent can simply be dropped.
        string normalized = label!.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new(normalized.Length);
        bool pendingSeparator = false;

        foreach (char character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            char lower = char.ToLowerInvariant(character);

            if ((lower >= 'a' && lower <= 'z') || (lower >= '0' && lower <= '9'))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append('_');
                }

                builder.Append(lower);
                pendingSeparator = false;
            }
            else
            {
                pendingSeparator = true;
            }
        }

        string slug = builder.ToString();
        return slug.Length == 0 ? FallbackKey : slug;
    }

    /// <summary>
    /// Returns the form with a unique key on every input. Explicit keys are honoured and only
    /// de-duplicated; missing keys are derived from the label. Duplicates get a <c>_2</c>,
    /// <c>_3</c> suffix in document order, so the first "Name" stays <c>name</c> and later
    /// ones become <c>name_2</c> — adding a field at the bottom of a form never renumbers the
    /// fields above it.
    /// </summary>
    public static FormDefinition Assign(FormDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        HashSet<string> used = new(StringComparer.Ordinal);

        IReadOnlyList<FormElement> elements = ElementTree.Rewrite(definition.Elements, element =>
        {
            bool needsKey = element is InputElement || !string.IsNullOrWhiteSpace(element.Key);
            if (!needsKey)
            {
                return element;
            }

            string desired = !string.IsNullOrWhiteSpace(element.Key)
                ? element.Key.Trim()
                : Slugify(KeySourceOf(element));

            string unique = MakeUnique(desired, used);
            return string.Equals(unique, element.Key, StringComparison.Ordinal)
                ? element
                : element with { Key = unique };
        });

        return definition with { Elements = elements };
    }

    /// <summary>Appends <c>_2</c>, <c>_3</c>… until the key is unused, then records it.</summary>
    public static string MakeUnique(string desired, HashSet<string> used)
    {
        if (used is null)
        {
            throw new ArgumentNullException(nameof(used));
        }

        string candidate = string.IsNullOrWhiteSpace(desired) ? FallbackKey : desired;

        if (used.Add(candidate))
        {
            return candidate;
        }

        for (int suffix = 2; suffix < int.MaxValue; suffix++)
        {
            string next = candidate + "_" + suffix.ToString(CultureInfo.InvariantCulture);
            if (used.Add(next))
            {
                return next;
            }
        }

        throw new InvalidOperationException($"Could not find a unique key for '{desired}'.");
    }

    /// <summary>
    /// The text a key is derived from. Most elements use their label, but a check box usually
    /// carries its wording in <c>Content</c> and a container in <c>Header</c>, and deriving
    /// "field_7" from those would be needlessly unhelpful.
    /// </summary>
    private static string? KeySourceOf(FormElement element)
    {
        if (!string.IsNullOrWhiteSpace(element.Label))
        {
            return element.Label;
        }

        return element switch
        {
            CheckBoxElement checkBox => checkBox.Content,
            GroupBoxElement groupBox => groupBox.Header,
            ExpanderElement expander => expander.Header,
            TabPageElement tabPage => tabPage.Header,
            CardElement card => card.Header,
            ButtonElement button => button.Text,
            LabelElement label => label.Text,
            _ => null,
        };
    }
}
