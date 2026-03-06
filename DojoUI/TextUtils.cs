using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace DojoUI;

public static class TextUtils
{
    /// <summary>
    /// Truncates text to fit within maxWidth pixels, appending ellipsis if needed.
    /// Returns the original string unchanged if it already fits.
    /// </summary>
    public static string TruncateToFit(SpriteFont font, string text, int maxWidth, string ellipsis = "...")
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0) return text ?? "";
        if (font.MeasureString(text).X <= maxWidth) return text;

        // Binary search for the longest prefix that fits with ellipsis
        int lo = 0, hi = text.Length;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (font.MeasureString(text[..mid] + ellipsis).X <= maxWidth)
                lo = mid;
            else
                hi = mid - 1;
        }

        return lo > 0 ? text[..lo] + ellipsis : ellipsis;
    }

    /// <summary>
    /// Word-wraps text to fit within maxWidth pixels. Breaks on spaces where
    /// possible; if a single word exceeds maxWidth, breaks mid-word.
    /// Returns a list of wrapped lines.
    /// </summary>
    public static List<string> WrapText(SpriteFont font, string text, int maxWidth)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text) || maxWidth <= 0)
        {
            lines.Add(text ?? "");
            return lines;
        }

        // If entire text fits on one line, skip the splitting work
        if (font.MeasureString(text).X <= maxWidth)
        {
            lines.Add(text);
            return lines;
        }

        var words = SplitKeepingSpaces(text);
        var currentLine = new System.Text.StringBuilder();

        foreach (var word in words)
        {
            string candidate = currentLine.Length == 0 ? word : currentLine.ToString() + word;
            if (font.MeasureString(candidate).X <= maxWidth)
            {
                currentLine.Append(word);
            }
            else if (currentLine.Length == 0)
            {
                // Single word too long — break it character by character
                BreakLongWord(font, word, maxWidth, lines);
            }
            else
            {
                // Flush current line, start new one with this word
                lines.Add(currentLine.ToString().TrimEnd());
                currentLine.Clear();

                // Check if word itself fits
                string trimmedWord = word.TrimStart();
                if (font.MeasureString(trimmedWord).X <= maxWidth)
                {
                    currentLine.Append(trimmedWord);
                }
                else
                {
                    BreakLongWord(font, trimmedWord, maxWidth, lines);
                }
            }
        }

        if (currentLine.Length > 0)
            lines.Add(currentLine.ToString().TrimEnd());

        return lines;
    }

    /// <summary>
    /// Splits text into tokens where each space is attached to the preceding word.
    /// "hello world test" → ["hello ", "world ", "test"]
    /// </summary>
    private static List<string> SplitKeepingSpaces(string text)
    {
        var tokens = new List<string>();
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ' && i + 1 < text.Length && text[i + 1] != ' ')
            {
                tokens.Add(text[start..(i + 1)]);
                start = i + 1;
            }
        }
        if (start < text.Length)
            tokens.Add(text[start..]);
        return tokens;
    }

    /// <summary>
    /// Breaks a word that's too long for one line into multiple lines at character boundaries.
    /// </summary>
    private static void BreakLongWord(SpriteFont font, string word, int maxWidth, List<string> lines)
    {
        int start = 0;
        while (start < word.Length)
        {
            // Binary search for the longest substring that fits
            int lo = start + 1, hi = word.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (font.MeasureString(word[start..mid]).X <= maxWidth)
                    lo = mid;
                else
                    hi = mid - 1;
            }
            // Always advance at least 1 character to avoid infinite loop
            if (lo == start) lo = start + 1;
            lines.Add(word[start..lo]);
            start = lo;
        }
    }
}
