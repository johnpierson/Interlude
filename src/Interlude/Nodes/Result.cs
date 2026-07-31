using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;
using Interlude.Model;
using Interlude.Runtime;

namespace Interlude;

/// <summary>
/// Reading a form's answers.
///
/// Every node here accepts either the <c>values</c> dictionary or the <c>form</c> output of
/// <c>Form.Show</c>, so it does not matter which one is to hand. They exist so a graph can say
/// what it expects — a number, a date, a colour — rather than pulling an object out of a
/// dictionary and hoping. Each one takes a fallback used when the field is missing or empty,
/// which is what keeps a downstream node from receiving a null it was not expecting.
/// </summary>
public class Result
{
    private Result()
    {
    }

    /// <summary>
    /// The raw answer for a field.
    /// </summary>
    /// <param name="result">The values dictionary or the form output of Form.Show.</param>
    /// <param name="key">The field to read.</param>
    /// <param name="fallback">Returned when the field is missing.</param>
    /// <returns name="value">The answer.</returns>
    /// <search>value,get,key,read,answer</search>
    public static object? ValueByKey(object result, string key, [DefaultArgument("null")] object? fallback = null)
    {
        IReadOnlyDictionary<string, object?> values = ValuesOf(result);
        return values.TryGetValue(key ?? string.Empty, out object? value) ? value ?? fallback : fallback;
    }

    /// <summary>
    /// A field's answer as text.
    /// </summary>
    /// <param name="result">The values dictionary or the form output of Form.Show.</param>
    /// <param name="key">The field to read.</param>
    /// <param name="fallback">Returned when the field is missing or empty.</param>
    /// <returns name="value">The answer as text.</returns>
    /// <search>string,text,get,read</search>
    public static string GetString(object result, string key, string fallback = "")
    {
        object? value = ValueByKey(result, key);
        return value is null || ValueOps.IsEmpty(value) ? fallback : ValueOps.ToStringInvariant(value);
    }

    /// <summary>
    /// A field's answer as a number.
    /// </summary>
    /// <param name="result">The values dictionary or the form output of Form.Show.</param>
    /// <param name="key">The field to read.</param>
    /// <param name="fallback">Returned when the field is missing or is not a number.</param>
    /// <returns name="value">The answer as a number.</returns>
    /// <search>number,double,numeric,get,read</search>
    public static double GetNumber(object result, string key, double fallback = 0)
        => ValueOps.TryToDouble(ValueByKey(result, key), out double number) ? number : fallback;

    /// <summary>
    /// A field's answer as a whole number.
    /// </summary>
    /// <param name="result">The values dictionary or the form output of Form.Show.</param>
    /// <param name="key">The field to read.</param>
    /// <param name="fallback">Returned when the field is missing or is not a number.</param>
    /// <returns name="value">The answer as a whole number.</returns>
    /// <search>integer,int,whole,count,get,read</search>
    public static int GetInteger(object result, string key, int fallback = 0)
        => ValueOps.TryToDouble(ValueByKey(result, key), out double number)
            ? (int)Math.Round(number, MidpointRounding.AwayFromZero)
            : fallback;

    /// <summary>
    /// A field's answer as true or false.
    /// </summary>
    /// <param name="result">The values dictionary or the form output of Form.Show.</param>
    /// <param name="key">The field to read.</param>
    /// <param name="fallback">Returned when the field is missing.</param>
    /// <returns name="value">The answer as a boolean.</returns>
    /// <search>bool,boolean,true,false,checkbox,get</search>
    public static bool GetBool(object result, string key, bool fallback = false)
    {
        object? value = ValueByKey(result, key);
        return value is null ? fallback : ValueOps.ToBool(value);
    }

    /// <summary>
    /// A field's answer as a date.
    /// </summary>
    /// <param name="result">The values dictionary or the form output of Form.Show.</param>
    /// <param name="key">The field to read.</param>
    /// <param name="fallback">Returned when the field is missing or empty.</param>
    /// <returns name="value">The answer as a date.</returns>
    /// <search>date,datetime,time,when,get,read</search>
    public static DateTime? GetDate(object result, string key, [DefaultArgument("null")] object? fallback = null)
    {
        object? value = ValueByKey(result, key);

        if (ValueOps.TryToDateTime(value, out DateTime date))
        {
            return date;
        }

        return ValueOps.TryToDateTime(fallback, out DateTime fallbackDate) ? fallbackDate : null;
    }

    /// <summary>
    /// A colour answer, broken out as hex and as numbers.
    /// </summary>
    /// <param name="result">The values dictionary or the form output of Form.Show.</param>
    /// <param name="key">The field to read.</param>
    /// <returns name="hex">The colour as "#RRGGBB", or "#AARRGGBB" when it is not fully opaque.</returns>
    /// <returns name="red">Red, 0 to 255.</returns>
    /// <returns name="green">Green, 0 to 255.</returns>
    /// <returns name="blue">Blue, 0 to 255.</returns>
    /// <returns name="alpha">Opacity, 0 to 255.</returns>
    /// <search>colour,color,hex,rgb,argb,get,read</search>
    [MultiReturn(new[] { "hex", "red", "green", "blue", "alpha" })]
    public static Dictionary<string, object> GetColor(object result, string key)
    {
        object? value = ValueByKey(result, key);

        RgbColor colour = value switch
        {
            RgbColor rgb => rgb,
            string text when RgbColor.TryParse(text, out RgbColor parsed) => parsed,
            _ => RgbColor.Black,
        };

        return new Dictionary<string, object>
        {
            ["hex"] = colour.ToHex(),
            ["red"] = (int)colour.Red,
            ["green"] = (int)colour.Green,
            ["blue"] = (int)colour.Blue,
            ["alpha"] = (int)colour.Alpha,
        };
    }

    /// <summary>
    /// A field's answer as a list. A single answer comes back as a one-item list, so a
    /// downstream node never has to care whether the field allowed several.
    /// </summary>
    /// <param name="result">The values dictionary or the form output of Form.Show.</param>
    /// <param name="key">The field to read.</param>
    /// <returns name="values">The answers as a list.</returns>
    /// <search>list,multiple,selection,items,get</search>
    public static List<object?> GetList(object result, string key)
        => ValueOps.AsList(ValueByKey(result, key)).ToList();

    /// <summary>
    /// A file or folder field's answer as a list of paths.
    /// </summary>
    /// <param name="result">The values dictionary or the form output of Form.Show.</param>
    /// <param name="key">The field to read.</param>
    /// <returns name="paths">The paths.</returns>
    /// <search>files,paths,filepaths,folder,get</search>
    public static List<string> GetFilePaths(object result, string key)
        => ValueOps.AsList(ValueByKey(result, key))
            .Select(ValueOps.ToStringInvariant)
            .Where(path => path.Length > 0)
            .ToList();

    /// <summary>
    /// Every field name in the answers.
    /// </summary>
    /// <param name="result">The values dictionary or the form output of Form.Show.</param>
    /// <returns name="keys">The field names.</returns>
    /// <search>keys,fields,names,list</search>
    public static List<string> Keys(object result)
        => ValuesOf(result).Keys.OrderBy(key => key, StringComparer.Ordinal).ToList();

    /// <summary>
    /// Every answer, in the same order as <c>Result.Keys</c>.
    /// </summary>
    /// <param name="result">The values dictionary or the form output of Form.Show.</param>
    /// <returns name="values">The answers.</returns>
    /// <search>values,answers,list</search>
    public static List<object?> Values(object result)
    {
        IReadOnlyDictionary<string, object?> values = ValuesOf(result);

        return values.Keys
            .OrderBy(key => key, StringComparer.Ordinal)
            .Select(key => values[key])
            .ToList();
    }

    /// <summary>
    /// Whether the user confirmed the form rather than cancelling it.
    /// </summary>
    /// <param name="result">The form output of Form.Show.</param>
    /// <returns name="wasSubmitted">True when the form was confirmed.</returns>
    /// <search>submitted,confirmed,ok,accepted</search>
    public static bool WasSubmitted(object result)
        => result is FormResult formResult && formResult.WasSubmitted;

    /// <summary>
    /// Whether the user cancelled or closed the form.
    /// </summary>
    /// <param name="result">The form output of Form.Show.</param>
    /// <returns name="wasCancelled">True when the form was cancelled.</returns>
    /// <search>cancelled,canceled,closed,dismissed,escaped</search>
    public static bool WasCancelled(object result) => !WasSubmitted(result);

    /// <summary>
    /// Which button ended the form: "submit", "cancel", "closed", "skipped", or a custom
    /// button's tag.
    /// </summary>
    /// <param name="result">The form output of Form.Show.</param>
    /// <returns name="buttonClicked">The button's name.</returns>
    /// <search>button,clicked,action,which</search>
    public static string ButtonClicked(object result)
        => result is FormResult formResult ? formResult.ButtonClicked : string.Empty;

    /// <summary>
    /// Whether the form has a field with this name. Useful when a graph reads a form loaded from
    /// JSON that it did not build itself.
    /// </summary>
    /// <param name="result">The values dictionary or the form output of Form.Show.</param>
    /// <param name="key">The field to look for.</param>
    /// <returns name="exists">True when the field is present.</returns>
    /// <search>has,contains,exists,key,field</search>
    public static bool HasKey(object result, string key)
        => ValuesOf(result).ContainsKey(key ?? string.Empty);

    /// <summary>
    /// Reads the answers from whichever shape they arrived in. Accepting both the dictionary and
    /// the result object costs a few lines here and saves every graph a conversion node.
    /// </summary>
    private static IReadOnlyDictionary<string, object?> ValuesOf(object? result)
    {
        switch (result)
        {
            case FormResult formResult:
                return formResult.Values;

            case IReadOnlyDictionary<string, object?> readOnly:
                return readOnly;

            case IDictionary<string, object> typed:
                return typed.ToDictionary(pair => pair.Key, pair => (object?)pair.Value, StringComparer.Ordinal);

            case System.Collections.IDictionary loose:
                Dictionary<string, object?> converted = new(StringComparer.Ordinal);
                foreach (System.Collections.DictionaryEntry entry in loose)
                {
                    converted[ValueOps.ToStringInvariant(entry.Key)] = entry.Value;
                }

                return converted;

            default:
                return new Dictionary<string, object?>(StringComparer.Ordinal);
        }
    }
}
