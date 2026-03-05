using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DojoUI;

/// <summary>
/// Immediate-mode form layout helper. Tracks a cursor Y position
/// and provides standardized row drawing methods.
/// Created per-frame in Draw() — no retained state between frames.
/// </summary>
public struct FormLayout
{
    /// <summary>Current Y cursor position (advances as rows are drawn).</summary>
    public int CursorY;

    /// <summary>Left edge of content area.</summary>
    public readonly int ContentX;

    /// <summary>Total content width.</summary>
    public readonly int ContentWidth;

    /// <summary>Width reserved for labels.</summary>
    public readonly int LabelWidth;

    /// <summary>Height of input fields.</summary>
    public readonly int FieldHeight;

    /// <summary>Vertical stride per row (field + gap).</summary>
    public readonly int RowHeight;

    /// <summary>X position where fields start.</summary>
    public int FieldX => ContentX + LabelWidth;

    /// <summary>Available width for fields (single-field rows).</summary>
    public int FieldWidth => ContentWidth - LabelWidth;

    public FormLayout(int contentX, int contentWidth, int startY,
                      int labelWidth = 110, int fieldHeight = 22, int rowHeight = 28)
    {
        ContentX = contentX;
        ContentWidth = contentWidth;
        CursorY = startY;
        LabelWidth = labelWidth;
        FieldHeight = fieldHeight;
        RowHeight = rowHeight;
    }

    /// <summary>
    /// Compute the Y offset to vertically center text within a field.
    /// </summary>
    public static int CenterTextY(int containerY, int containerHeight, int textHeight)
        => containerY + (containerHeight - textHeight) / 2;

    /// <summary>
    /// Draw a standard label + TextInputField row. Returns the field Rectangle.
    /// Advances CursorY by RowHeight.
    /// </summary>
    public Rectangle DrawLabeledField(SpriteBatch sb, SpriteFont font, Renderer renderer,
        string label, TextInputField field, GameTime gt, Color labelColor)
    {
        int labelY = CenterTextY(CursorY, FieldHeight, font.LineSpacing);
        sb.DrawString(font, label, new Vector2(ContentX, labelY), labelColor);

        var rect = new Rectangle(FieldX, CursorY, FieldWidth, FieldHeight);
        field.Draw(sb, font, renderer, rect, gt);

        CursorY += RowHeight;
        return rect;
    }

    /// <summary>
    /// Draw a standard label + Dropdown row. Returns the dropdown Rectangle.
    /// Advances CursorY by RowHeight.
    /// </summary>
    public Rectangle DrawLabeledDropdown(SpriteBatch sb, SpriteFont font, Renderer renderer,
        string label, Dropdown dropdown, Color labelColor, int dropdownWidth = 0)
    {
        int labelY = CenterTextY(CursorY, FieldHeight, font.LineSpacing);
        sb.DrawString(font, label, new Vector2(ContentX, labelY), labelColor);

        int ddW = dropdownWidth > 0 ? dropdownWidth : FieldWidth;
        var rect = new Rectangle(FieldX, CursorY, ddW, FieldHeight);
        dropdown.Draw(sb, font, renderer, rect);

        CursorY += RowHeight;
        return rect;
    }

    /// <summary>
    /// Draw a standard label + NumericField row. Returns the field Rectangle.
    /// Advances CursorY by RowHeight.
    /// </summary>
    public Rectangle DrawLabeledNumeric(SpriteBatch sb, SpriteFont font, Renderer renderer,
        string label, NumericField field, GameTime gt, Color labelColor, int numericWidth = 80)
    {
        int labelY = CenterTextY(CursorY, FieldHeight, font.LineSpacing);
        sb.DrawString(font, label, new Vector2(ContentX, labelY), labelColor);

        var rect = new Rectangle(FieldX, CursorY, numericWidth, FieldHeight);
        field.Draw(sb, font, renderer, rect, gt);

        CursorY += RowHeight;
        return rect;
    }

    /// <summary>
    /// Draw a standard label + Checkbox row. Returns the checkbox Rectangle.
    /// Advances CursorY by RowHeight.
    /// </summary>
    public Rectangle DrawLabeledCheckbox(SpriteBatch sb, SpriteFont font, Renderer renderer,
        string label, Checkbox checkbox, Color labelColor, int checkboxWidth = 22)
    {
        int labelY = CenterTextY(CursorY, FieldHeight, font.LineSpacing);
        sb.DrawString(font, label, new Vector2(ContentX, labelY), labelColor);

        var rect = new Rectangle(FieldX, CursorY, checkboxWidth, FieldHeight);
        checkbox.Draw(sb, renderer, rect);

        CursorY += RowHeight;
        return rect;
    }

    /// <summary>
    /// Draw two TextInputFields side by side on one row.
    /// Labels are measured from font to prevent truncation.
    /// Returns (leftRect, rightRect). Advances CursorY by RowHeight.
    /// </summary>
    public (Rectangle Left, Rectangle Right) DrawTwoFieldRow(
        SpriteBatch sb, SpriteFont font, Renderer renderer,
        string leftLabel, TextInputField leftField,
        string rightLabel, TextInputField rightField,
        GameTime gt, Color labelColor, int gap = 12)
    {
        int labelY = CenterTextY(CursorY, FieldHeight, font.LineSpacing);

        // Measure label widths from actual font metrics
        int leftLabelW = (int)font.MeasureString(leftLabel).X + 6;
        int rightLabelW = (int)font.MeasureString(rightLabel).X + 6;

        // Available width for the two fields after subtracting labels and gap
        int totalAvail = ContentWidth - leftLabelW - rightLabelW - gap;
        int halfFieldW = totalAvail / 2;

        // Left: label + field
        int lx = ContentX;
        sb.DrawString(font, leftLabel, new Vector2(lx, labelY), labelColor);
        var leftRect = new Rectangle(lx + leftLabelW, CursorY, halfFieldW, FieldHeight);
        leftField.Draw(sb, font, renderer, leftRect, gt);

        // Right: label + field
        int rx = leftRect.Right + gap;
        sb.DrawString(font, rightLabel, new Vector2(rx, labelY), labelColor);
        var rightRect = new Rectangle(rx + rightLabelW, CursorY, halfFieldW, FieldHeight);
        rightField.Draw(sb, font, renderer, rightRect, gt);

        CursorY += RowHeight;
        return (leftRect, rightRect);
    }

    /// <summary>
    /// Draw a section header label (e.g., "Nodes", "Objectives").
    /// Advances CursorY by RowHeight.
    /// </summary>
    public void DrawSectionHeader(SpriteBatch sb, SpriteFont font, string text, Color color)
    {
        int labelY = CenterTextY(CursorY, FieldHeight, font.LineSpacing);
        sb.DrawString(font, text, new Vector2(ContentX, labelY), color);
        CursorY += RowHeight;
    }

    /// <summary>
    /// Draw a horizontal separator line. Advances CursorY by height.
    /// </summary>
    public void DrawSeparator(SpriteBatch sb, Renderer renderer, Color color, int height = 8)
    {
        int lineY = CursorY + height / 2;
        renderer.DrawRect(sb, new Rectangle(ContentX, lineY, ContentWidth, 1), color);
        CursorY += height;
    }

    /// <summary>
    /// Add vertical spacing without drawing anything.
    /// </summary>
    public void Space(int pixels)
    {
        CursorY += pixels;
    }
}
