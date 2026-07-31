using System.Collections.Generic;
using Interlude.Model;

namespace Interlude.Tests;

/// <summary>Small builders so tests read as intent rather than as object initialisers.</summary>
internal static class TestForms
{
    internal static FormDefinition Form(params FormElement[] elements)
        => new FormDefinition { Title = "Test", Elements = elements }.WithResolvedKeys();

    internal static TextBoxElement Text(string key, string? defaultValue = null)
        => new() { Key = key, Label = key, DefaultValue = defaultValue };

    internal static NumericElement Number(string key, double defaultValue = 0d)
        => new() { Key = key, Label = key, DefaultValue = defaultValue };

    internal static CheckBoxElement Check(string key, bool defaultValue = false)
        => new() { Key = key, Label = key, DefaultValue = defaultValue };

    internal static DropdownElement Dropdown(string key, params object[] values)
        => new()
        {
            Key = key,
            Label = key,
            Options = OptionItem.Pair(values, null),
        };

    internal static IReadOnlyList<OptionItem> Options(params string[] values)
        => OptionItem.Pair(values, null);
}
