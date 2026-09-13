using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;
using Interlude.Validation;

namespace Interlude.Serialization;

/// <summary>
/// Turns a form into JSON and back.
///
/// This is the package's strategic contract, not a convenience. Once a form is data, it can be
/// checked into a repository, reviewed in a pull request, diffed between releases, handed to the
/// preview harness, replayed in a test, and one day rendered by something that is not WPF.
/// Everything else in Interlude is an implementation of this document.
///
/// Uses the in-box <c>System.Text.Json</c> deliberately: adding Newtonsoft would mean fighting
/// whichever version Dynamo has already loaded, which is precisely the class of problem this
/// package exists to avoid.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public static class FormJson
{
    private static readonly JsonSerializerOptions WriteOptions = CreateOptions(indented: true);
    private static readonly JsonSerializerOptions CompactOptions = CreateOptions(indented: false);

    /// <summary>The options used for reading and for indented writing.</summary>
    internal static JsonSerializerOptions Options => WriteOptions;

    /// <summary>Writes a form as JSON.</summary>
    public static string Serialize(FormDefinition definition, bool indented = true)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        EnsureNoCustomRules(definition);
        try
        {
            return JsonSerializer.Serialize(definition, indented ? WriteOptions : CompactOptions);
        }
        catch (JsonException ex)
        {
            throw new InterludeJsonException($"This form could not be written: {ex.Message}", ex);
        }
    }

    /// <summary>Reads a form from JSON.</summary>
    /// <exception cref="InterludeJsonException">The document is malformed or too new to read.</exception>
    public static FormDefinition Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InterludeJsonException("There is no form here: the JSON is empty.");
        }

        // Check the version before attempting the full read, so a document written by a newer
        // Interlude produces "this file is newer than this version" rather than a baffling
        // complaint about an unknown $type three levels down.
        EnsureReadableVersion(json);

        try
        {
            FormDefinition? definition = JsonSerializer.Deserialize<FormDefinition>(json, WriteOptions);
            if (definition is null)
            {
                throw new InterludeJsonException("There is no form here: the JSON was null.");
            }

            EnsureNoCustomRules(definition);
            return definition;
        }
        catch (JsonException ex)
        {
            throw new InterludeJsonException($"This form could not be read: {ex.Message}", ex);
        }
    }

    /// <summary>Reads a form from a file.</summary>
    public static FormDefinition Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A file path is required.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"No form file at '{path}'.", path);
        }

        return Deserialize(File.ReadAllText(path));
    }

    /// <summary>Writes a form to a file.</summary>
    public static void Save(FormDefinition definition, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A file path is required.", nameof(path));
        }

        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, Serialize(definition));
    }

    /// <summary>Writes a form's answers as JSON.</summary>
    public static string SerializeValues(IReadOnlyDictionary<string, object?> values, bool indented = true)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        return JsonSerializer.Serialize(values, indented ? WriteOptions : CompactOptions);
    }

    /// <summary>Reads a set of answers from JSON, for pre-filling a form.</summary>
    public static Dictionary<string, object?> DeserializeValues(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, WriteOptions)
                   ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        }
        catch (JsonException ex)
        {
            throw new InterludeJsonException($"These form values could not be read: {ex.Message}", ex);
        }
    }

    private static void EnsureReadableVersion(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });

            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in document.RootElement.EnumerateObject())
                {
                    if (!string.Equals(property.Name, "schemaVersion", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (property.Value.TryGetInt32(out int schemaVersion) &&
                        schemaVersion > FormDefinition.CurrentSchemaVersion)
                    {
                        throw new InterludeJsonException(
                            $"This form uses schema version {schemaVersion}, but this build of Interlude " +
                            $"understands version {FormDefinition.CurrentSchemaVersion}. Update Interlude to open it.");
                    }

                    break;
                }
            }
        }
        catch (JsonException ex)
        {
            throw new InterludeJsonException($"This is not valid JSON: {ex.Message}", ex);
        }
    }

    private static void EnsureNoCustomRules(FormDefinition definition)
    {
        if (definition.AllElements().Any(element => element.Rules.Any(rule => rule is CustomPredicateRule)))
        {
            throw new InterludeJsonException(
                "Custom validation rules contain executable code and cannot be serialized or restored from JSON.");
        }
    }

    private static JsonSerializerOptions CreateOptions(bool indented) => new()
    {
        WriteIndented = indented,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,

        // Only nulls are omitted. Skipping *default* values would be smaller but wrong: a
        // property whose initializer is `true` could never be written as `false`, because the
        // reader would restore the initializer.
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

        // Enums as names, so a form file reads like a description of a form.
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new LooseValueConverter(),
        },

        // Relaxed escaping keeps accented labels legible instead of turning "Höhe" into "Höhe".
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}

/// <summary>Raised when a form file cannot be read.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class InterludeJsonException : Runtime.InterludeException
{
    public InterludeJsonException(string message)
        : base(message)
    {
    }

    public InterludeJsonException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
