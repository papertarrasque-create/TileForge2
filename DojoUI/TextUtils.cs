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
}
