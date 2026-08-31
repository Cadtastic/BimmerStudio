using System.Text.RegularExpressions;

namespace BimmerStudio.SgbdInventory;

/// <summary>What a comment line is, for the purpose of judging translation coverage.</summary>
public enum PhraseClass
{
    /// <summary>The dictionary translates it.</summary>
    Translated,

    /// <summary>A protocol service name or an EDIABAS table reference.</summary>
    ProtocolOrTable,

    /// <summary>A bare identifier, job-name token, hex value or ASCII table rule.</summary>
    Identifier,

    /// <summary>Written in English already.</summary>
    AlreadyEnglish,

    /// <summary>German prose with no translation. The only category that is a real gap.</summary>
    UntranslatedGerman,
}

/// <summary>
/// Sorts comment lines into what a reader can and cannot read.
/// </summary>
/// <remarks>
/// Raw "percent translated" is a misleading measure of this corpus. Roughly half of every
/// untranslated remainder is text that must never be translated — KWP2000 and UDS service names,
/// EDIABAS table references, job-name tokens and hex — and roughly half again is written in
/// English by BMW to begin with. Counting those as gaps made a finished job list look 55% done.
/// <para>
/// The classifier is a heuristic and is honest about it: it is reliable in aggregate, but an
/// individual line can be misfiled either way, so it reports counts rather than making decisions.
/// </para>
/// </remarks>
public static partial class PhraseClassifier
{
    [GeneratedRegex(@"^\s*(KWP\s?2000|UDS)\s*[:.]|^\s*table\s|^\s*[-|+]{3,}", RegexOptions.IgnoreCase)]
    private static partial Regex ProtocolOrTablePattern();

    /// <summary>A bare hex token, an ALL_CAPS job name, or an identifier with a hex address.</summary>
    [GeneratedRegex(@"^\$[0-9A-Fa-f]+\s*$|^[A-Z0-9_]{3,}(\s*\(0x[0-9A-Fa-f ]+\))?\s*$")]
    private static partial Regex IdentifierPattern();

    /// <summary>Nothing but punctuation, digits and hex.</summary>
    [GeneratedRegex(@"^[\s$0-9A-Fa-fx.,:;()\[\]/|+-]+$")]
    private static partial Regex SymbolsPattern();

    /// <summary>
    /// German function words and stems. Deliberately excludes cognates that also read as English.
    /// </summary>
    [GeneratedRegex(
        @"\b(der|die|das|des|dem|den|und|nicht|wird|werden|von|zur|zum|für|fuer|mit|aus|bei"
        + @"|eine|einen|kein|keine|lesen|schreiben|loeschen|löschen|auslesen|setzen|ueber|über"
        + @"|nach|beim|wenn|durch|Anzahl|Wert|Fehler|Daten|Zustand|Steuerger|schalten|Meldung)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex GermanPattern();

    /// <summary>
    /// English markers. A line carrying these is treated as English even when a German-looking
    /// token appears in it, because "status", "control" and "data" are shared between the two.
    /// </summary>
    [GeneratedRegex(
        @"\b(the|and|of|for|is|are|read|reads|write|writes|status|returns|control|controls"
        + @"|value|error|data|with|from|this|that|switch|enable|disable)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex EnglishPattern();

    public static PhraseClass Classify(string phrase, bool isTranslated)
    {
        if (isTranslated)
        {
            return PhraseClass.Translated;
        }

        if (ProtocolOrTablePattern().IsMatch(phrase))
        {
            return PhraseClass.ProtocolOrTable;
        }

        if (IdentifierPattern().IsMatch(phrase) || SymbolsPattern().IsMatch(phrase))
        {
            return PhraseClass.Identifier;
        }

        // German only when nothing English-looking is present: the overlap between the two
        // vocabularies is where a naive classifier goes wrong.
        return GermanPattern().IsMatch(phrase) && !EnglishPattern().IsMatch(phrase)
            ? PhraseClass.UntranslatedGerman
            : PhraseClass.AlreadyEnglish;
    }

    /// <summary>
    /// True when a reader can read the line as it stands: either the dictionary translated it, or
    /// it is something that was never going to be translated.
    /// </summary>
    public static bool IsReadable(this PhraseClass phraseClass) =>
        phraseClass != PhraseClass.UntranslatedGerman;
}
