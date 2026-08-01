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
    /// The raw answer for a field, exactly as the form produced it.
    ///
    /// The escape hatch. Every other node here promises a type; this one promises nothing and
    /// hands over whatever is there — which is what you want for a choice input holding Revit
    /// elements, where converting to anything would lose them.
    ///
    /// For everything else prefer the typed accessors. `Result.GetNumber` on a field the user left
    /// empty gives you the fallback you chose; this gives you whatever emptiness looked like, and
    /// the node three steps downstream is where you find out.
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
    ///
    /// Works on any field, not just text ones: a number comes back as its printed form, a choice
    /// as the display name of what was chosen. Handy for building a filename or a parameter value
    /// out of whatever the user picked.
    ///
    /// A missing or empty field gives the fallback, so the answer is never null and never needs
    /// guarding before it is concatenated.
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
    ///
    /// Text that looks like a number is converted, so this reads a text box the user typed "3.5"
    /// into as well as it reads a numeric field. Text that does not look like a number gives the
    /// fallback rather than failing the graph.
    ///
    /// The conversion accepts the machine's own decimal separator, so a comma-decimal locale
    /// reads its own numbers correctly rather than losing the fractional part.
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
    ///
    /// A fractional answer is rounded rather than truncated, so 2.6 becomes 3. Use this for
    /// anything that indexes or counts, where a number carrying a hidden .0 causes trouble
    /// downstream.
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
    ///
    /// Reads a tick box or a switch directly, and also makes sense of the text and numbers a
    /// loaded form might carry — "true", "yes", "1" are true; "false", "no", "0" are false.
    /// Anything it cannot make sense of gives the fallback.
    ///
    /// This is what gates the rest of a graph, so choose the fallback as the safe answer: the
    /// value you would want if the field turned out not to be there at all.
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
    ///
    /// A date field left empty answers with nothing at all — it is the one input whose answer can
    /// genuinely be absent — so the fallback here earns its keep more than most.
    ///
    /// Dates stored as text are read back in a culture-independent form, which is what lets a form
    /// written in one region load correctly in another. Only what the user *sees* follows the
    /// machine's own format.
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
    /// A colour answer, broken out as hex and as numbers on separate ports.
    ///
    /// Both forms come out at once — the hex string, and red, green, blue and alpha as numbers
    /// from 0 to 255 — because the node that wants a colour next might want either, and
    /// converting between them in a graph is tedious.
    ///
    /// Take the numbers to build a Revit or Dynamo colour; take the hex to write a parameter, a
    /// filename or a stylesheet.
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
    ///
    /// **Always a list**, whether the field allowed several files or exactly one, so the graph
    /// downstream is written the same way either time and does not break when the field is later
    /// changed to accept more.
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
    ///
    /// What a form actually produced, which is the quickest way to find out why
    /// <c>Result.GetString</c> is handing back its fallback: the key you asked for is not in this
    /// list, usually because it was derived from a label that has since been reworded.
    ///
    /// Pairs with <c>Result.Values</c>, which returns the answers in the same order.
    /// </summary>
    /// <param name="result">The values dictionary or the form output of Form.Show.</param>
    /// <returns name="keys">The field names.</returns>
    /// <search>keys,fields,names,list</search>
    public static List<string> Keys(object result)
        => ValuesOf(result).Keys.OrderBy(key => key, StringComparer.Ordinal).ToList();

    /// <summary>
    /// Every answer, in the same order as <c>Result.Keys</c>.
    ///
    /// The two lists line up index for index, so zipping them gives name-and-answer pairs — which
    /// is how you write every answer to a parameter, or a log, without naming the fields one by
    /// one in the graph.
    ///
    /// The answers come back as they are, untyped, so a graph that needs a particular type should
    /// name the field and use the accessor for it.
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
    ///
    /// **Test this before acting on the answers.** It is the whole of the cancellation contract:
    /// a cancelled form still returns every field's default value rather than nulls, so the
    /// answers always look usable and nothing downstream will fail to warn you. What tells the two
    /// apart is this flag and nothing else.
    ///
    /// The same value comes out of <c>Form.Show</c> directly; this node exists for reading it back
    /// off the <c>form</c> output further down a graph.
    /// </summary>
    /// <param name="result">The form output of Form.Show.</param>
    /// <returns name="wasSubmitted">True when the form was confirmed.</returns>
    /// <search>submitted,confirmed,ok,accepted</search>
    public static bool WasSubmitted(object result)
        => result is FormResult formResult && formResult.WasSubmitted;

    /// <summary>
    /// Whether the user cancelled or closed the form.
    ///
    /// The opposite of <c>Result.WasSubmitted</c>, for the graph that reads better as "stop if
    /// cancelled" than as "continue if submitted". Closing the window with its X counts as
    /// cancelling, as does a run skipped by a false <c>trigger</c>.
    /// </summary>
    /// <param name="result">The form output of Form.Show.</param>
    /// <returns name="wasCancelled">True when the form was cancelled.</returns>
    /// <search>cancelled,canceled,closed,dismissed,escaped</search>
    public static bool WasCancelled(object result) => !WasSubmitted(result);

    /// <summary>
    /// Which button ended the form: "submit", "cancel", "closed", "skipped", or a custom
    /// button's tag.
    ///
    /// How one form offers several outcomes. Add buttons with <c>Layout.Button</c>, give each a
    /// distinct tag, and branch on what comes back here — "Place", "Place and continue" and
    /// "Preview only" from a single dialog.
    ///
    /// The four built-in values are worth telling apart: "closed" is the window's X, and "skipped"
    /// means the dialog never appeared because <c>trigger</c> was false and the last answers were
    /// returned instead.
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
