using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

/// <summary>
/// Dialog that shows a list of recent files for quick selection.
/// </summary>
public class RecentFilesDialog : IDialog
{
    private static readonly Color OverlayColor = new(0, 0, 0, 160);
    private static readonly Color PanelColor = new(40, 40, 40);
    private static readonly Color PanelBorder = new(100, 100, 100);
    private static readonly Color ItemColor = new(50, 50, 50);
    private static readonly Color ItemHoverColor = new(60, 75, 110);
    private static readonly Color ItemTextColor = new(200, 200, 200);
    private static readonly Color ItemPathColor = new(120, 120, 120);
    private static readonly Color HintColor = new(140, 140, 140);
    private static readonly Color TitleColor = new(220, 220, 220);

    private const int PanelWidth = 500;
    private const int ItemHeight = 28;
    private const int Padding = 8;
    private const int TitleHeight = 30;

    private readonly List<string> _files;
    private int _hoverIndex = -1;
    private int _scrollOffset;

    public bool IsComplete { get; private set; }
    public bool WasCancelled { get; private set; }

    /// <summary>The selected file path, or null if cancelled.</summary>
    public string SelectedPath { get; private set; }

    public RecentFilesDialog(List<string> files)
    {
        _files = files ?? new();
    }

    public void Update(KeyboardState keyboard, KeyboardState prevKeyboard, GameTime gameTime)
    {
        if (KeyPressed(keyboard, prevKeyboard, Keys.Escape))
        {
            IsComplete = true;
            WasCancelled = true;
        }
    }

    public void OnTextInput(char character) { }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                     int screenWidth, int screenHeight, GameTime gameTime)
    {
        renderer.DrawRect(spriteBatch, new Rectangle(0, 0, screenWidth, screenHeight), OverlayColor);

        int maxVisible = Math.Min(_files.Count, 8);
        int panelHeight = TitleHeight + maxVisible * ItemHeight + Padding * 2 + 20; // 20 for hint
        int px = (screenWidth - PanelWidth) / 2;
        int py = (screenHeight - panelHeight) / 2;

        var panel = new Rectangle(px, py, PanelWidth, panelHeight);
        renderer.DrawRect(spriteBatch, panel, PanelColor);
        renderer.DrawRectOutline(spriteBatch, panel, PanelBorder, 1);

        // Title
        spriteBatch.DrawString(font, "Recent Projects", new Vector2(px + Padding, py + Padding), TitleColor);

        // Items
        int itemY = py + TitleHeight;
        var mouse = Mouse.GetState();
        _hoverIndex = -1;

        for (int i = _scrollOffset; i < _files.Count && i - _scrollOffset < maxVisible; i++)
        {
            var itemRect = new Rectangle(px + Padding, itemY, PanelWidth - Padding * 2, ItemHeight);
            bool hover = itemRect.Contains(mouse.X, mouse.Y);
            if (hover) _hoverIndex = i;

            renderer.DrawRect(spriteBatch, itemRect, hover ? ItemHoverColor : ItemColor);

            string fileName = Path.GetFileName(_files[i]);
            string dirName = Path.GetDirectoryName(_files[i]) ?? "";
            // Truncate dir if too long
            if (dirName.Length > 50) dirName = "..." + dirName[^47..];

            spriteBatch.DrawString(font, fileName, new Vector2(itemRect.X + 6, itemRect.Y + 2), ItemTextColor);
            spriteBatch.DrawString(font, dirName, new Vector2(itemRect.X + 6, itemRect.Y + font.LineSpacing), ItemPathColor);

            itemY += ItemHeight;
        }

        // Handle click
        if (mouse.LeftButton == ButtonState.Pressed && _hoverIndex >= 0)
        {
            SelectedPath = _files[_hoverIndex];
            IsComplete = true;
            WasCancelled = false;
        }

        // Hint
        string hint = _files.Count == 0 ? "No recent projects" : "[Click] Open    [Esc] Cancel";
        var hintSize = font.MeasureString(hint);
        spriteBatch.DrawString(font, hint,
            new Vector2(px + PanelWidth - hintSize.X - Padding, py + panelHeight - font.LineSpacing - 6), HintColor);
    }

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key)
        => current.IsKeyDown(key) && prev.IsKeyUp(key);
}
