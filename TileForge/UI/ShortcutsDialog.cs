using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;

namespace TileForge.UI;

/// <summary>
/// A read-only dialog that displays all keyboard shortcuts grouped by category.
/// Press Escape or Enter to close.
/// </summary>
public class ShortcutsDialog : IDialog
{
    // ---- Colors ----
    private static readonly Color OverlayColor     = new(0, 0, 0, 160);
    private static readonly Color PanelColor       = new(35, 35, 35);
    private static readonly Color PanelBorder      = new(80, 80, 80);
    private static readonly Color HeaderBg         = new(45, 45, 45);
    private static readonly Color CategoryColor    = new(140, 180, 220);
    private static readonly Color ShortcutColor    = Color.White;
    private static readonly Color DescriptionColor = new(160, 160, 160);
    private static readonly Color HintColor        = new(140, 140, 140);

    // ---- Layout ----
    private const int PanelWidth      = 500;
    private const int MaxPanelHeight  = 400;
    private const int HeaderHeight    = 30;
    private const int FooterHeight    = 24;
    private const int Padding         = 10;
    private const int LineHeight      = 18;
    private const int CategorySpacing = 8;  // extra gap before a category header
    private const int ScrollStep      = LineHeight * 2;

    // ---- Dot-fill format constants ----
    private const int ShortcutColumnWidth  = 16; // characters reserved for shortcut + dots
    private const string DotFill           = "................................................................................";

    // ---- State ----
    public bool IsComplete  { get; private set; }
    public bool WasCancelled { get; private set; }

    private int _scrollOffset;
    private int _maxScroll;

    // ---- Shortcut data ----
    private static readonly (string Category, string Shortcut, string Description)[] _shortcuts =
    {
        // File
        ("File", "Ctrl+N",       "New"),
        ("File", "Ctrl+O",       "Open"),
        ("File", "Ctrl+Shift+O", "Open Recent"),
        ("File", "Ctrl+S",       "Save"),
        ("File", "Ctrl+E",       "Export"),

        // Edit
        ("Edit", "Ctrl+Z", "Undo"),
        ("Edit", "Ctrl+Y", "Redo"),
        ("Edit", "Ctrl+R", "Resize Map"),
        ("Edit", "Del",    "Delete"),

        // View
        ("View", "Ctrl+M", "Toggle Minimap"),
        ("View", "G",      "Cycle Grid"),
        ("View", "V",      "Toggle Layer Visibility"),
        ("View", "Tab",    "Next Layer"),

        // Tools
        ("Tools", "B", "Brush"),
        ("Tools", "E", "Eraser"),
        ("Tools", "F", "Fill"),
        ("Tools", "N", "Entity Placer"),
        ("Tools", "I", "Tile Picker"),
        ("Tools", "M", "Selection"),

        // Play
        ("Play", "F5", "Play/Stop"),
    };

    /// <summary>
    /// Returns the full shortcut table for external inspection and testing.
    /// </summary>
    public static (string Category, string Shortcut, string Description)[] GetShortcuts()
        => _shortcuts;

    // ---- IDialog ----

    public void OnTextInput(char character) { }

    public void Update(KeyboardState keyboard, KeyboardState prevKeyboard, GameTime gameTime)
    {
        if (KeyPressed(keyboard, prevKeyboard, Keys.Escape) ||
            KeyPressed(keyboard, prevKeyboard, Keys.Enter))
        {
            IsComplete = true;
            WasCancelled = false;
            return;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Up))
            _scrollOffset = Math.Max(0, _scrollOffset - ScrollStep);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Down))
            _scrollOffset = Math.Min(_maxScroll, _scrollOffset + ScrollStep);
        if (KeyPressed(keyboard, prevKeyboard, Keys.PageUp))
            _scrollOffset = Math.Max(0, _scrollOffset - MaxPanelHeight);
        if (KeyPressed(keyboard, prevKeyboard, Keys.PageDown))
            _scrollOffset = Math.Min(_maxScroll, _scrollOffset + MaxPanelHeight);
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                     int screenWidth, int screenHeight, GameTime gameTime)
    {
        // Dim the background
        renderer.DrawRect(spriteBatch, new Rectangle(0, 0, screenWidth, screenHeight), OverlayColor);

        // Measure total content height
        int totalContentHeight = MeasureContentHeight(font);
        int contentAreaHeight = Math.Min(totalContentHeight, MaxPanelHeight - HeaderHeight - FooterHeight - Padding * 2);
        int panelHeight = HeaderHeight + FooterHeight + Padding * 2 + contentAreaHeight;
        panelHeight = Math.Min(panelHeight, MaxPanelHeight);

        // Recompute max scroll from actual measured content
        int visibleContentH = panelHeight - HeaderHeight - FooterHeight - Padding * 2;
        _maxScroll = Math.Max(0, totalContentHeight - visibleContentH);
        _scrollOffset = Math.Min(_scrollOffset, _maxScroll);

        int px = (screenWidth  - PanelWidth) / 2;
        int py = (screenHeight - panelHeight) / 2;
        var panel = new Rectangle(px, py, PanelWidth, panelHeight);

        // Panel background
        renderer.DrawRect(spriteBatch, panel, PanelColor);

        // Header
        var headerRect = new Rectangle(px, py, PanelWidth, HeaderHeight);
        renderer.DrawRect(spriteBatch, headerRect, HeaderBg);
        string title = "Keyboard Shortcuts";
        var titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title,
            new Vector2(px + (PanelWidth - titleSize.X) / 2, py + (HeaderHeight - font.LineSpacing) / 2),
            Color.White);

        // Content — scissor-clipped vertically
        int contentLeft   = px + Padding;
        int contentTop    = py + HeaderHeight + Padding;
        int contentBottom = py + panelHeight - FooterHeight - Padding;
        int contentRight  = px + PanelWidth - Padding;
        int contentWidth  = contentRight - contentLeft;

        // Draw each shortcut row, offsetting by _scrollOffset
        int y = contentTop - _scrollOffset;
        string currentCategory = null;

        foreach (var (category, shortcut, description) in _shortcuts)
        {
            // Category header
            if (category != currentCategory)
            {
                currentCategory = category;

                if (y >= contentTop - LineHeight && y <= contentBottom)
                {
                    spriteBatch.DrawString(font, category,
                        new Vector2(contentLeft, y), CategoryColor);
                }
                y += LineHeight + CategorySpacing / 2;
            }

            // Shortcut row
            if (y >= contentTop - LineHeight && y <= contentBottom)
            {
                string formatted = FormatShortcutLine(shortcut, description);
                spriteBatch.DrawString(font, formatted,
                    new Vector2(contentLeft + 8, y), ShortcutColor);
            }
            y += LineHeight;
        }

        // Panel border
        renderer.DrawRectOutline(spriteBatch, panel, PanelBorder, 1);

        // Footer hint
        string hint = _maxScroll > 0
            ? "[Enter/Esc] Close    [\u2191\u2193] Scroll"
            : "[Enter] or [Esc] to close";
        var hintSize = font.MeasureString(hint);
        int hintY = py + panelHeight - FooterHeight + (FooterHeight - font.LineSpacing) / 2;
        spriteBatch.DrawString(font, hint,
            new Vector2(px + (PanelWidth - hintSize.X) / 2, hintY), HintColor);
    }

    // ---- Helpers ----

    /// <summary>
    /// Formats one shortcut entry as "Ctrl+S ............ Save" with dot padding.
    /// </summary>
    private static string FormatShortcutLine(string shortcut, string description)
    {
        // Total display width (in characters) for the shortcut + dots portion
        int dotCount = Math.Max(1, ShortcutColumnWidth - shortcut.Length);
        string dots = " " + DotFill[..Math.Min(dotCount, DotFill.Length)] + " ";
        return shortcut + dots + description;
    }

    /// <summary>
    /// Measures the total pixel height needed to draw all rows (without scroll).
    /// </summary>
    private static int MeasureContentHeight(SpriteFont font)
    {
        int lineH = LineHeight;
        int total = 0;
        string prevCategory = null;

        foreach (var (category, _, _) in _shortcuts)
        {
            if (category != prevCategory)
            {
                prevCategory = category;
                // Category header row + half spacing
                total += lineH + CategorySpacing / 2;
            }
            total += lineH;
        }

        return total;
    }

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key)
        => current.IsKeyDown(key) && prev.IsKeyUp(key);
}
