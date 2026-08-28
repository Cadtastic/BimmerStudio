using System.Text;

namespace BimmerStudio.Infrastructure.Localization;

/// <summary>
/// Whitespace normalisation shared by the phrase dictionary's writer and its lookup.
/// </summary>
public static class TextNormaliser
{
    /// <summary>Trims and collapses interior whitespace runs to single spaces.</summary>
    public static string NormaliseWhitespace(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        var pendingSpace = false;

        foreach (var character in text.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace && builder.Length > 0)
            {
                builder.Append(' ');
            }

            pendingSpace = false;
            builder.Append(character);
        }

        return builder.ToString();
    }
}
