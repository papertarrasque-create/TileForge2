using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

public enum FileBrowserMode { Open, Save }

public class FileBrowserDialog : IDialog
{
    private static readonly Color OverlayColor = new(0, 0, 0, 160);
    private static readonly Color PanelColor = new(40, 40, 40);
    private static readonly Color PanelBorder = new(100, 100, 100);
    private static readonly Color PathColor = new(130, 160, 200);
    private static readonly Color HintColor = new(140, 140, 140);
    private static readonly Color SelectedColor = new(60, 80, 120);
    private static readonly Color HoverColor = new(55, 55, 65);
    private static readonly Color DirColor = new(180, 200, 230);
    private static readonly Color FileColor = new(200, 200, 200);
    private static readonly Color SeparatorColor = new(70, 70, 70);
    private static readonly Color ScrollBarColor = new(80, 80, 80);
    private static readonly Color ScrollThumbColor = new(120, 120, 120);

    private const int PanelWidth = 600;
    private const int PanelHeightOpen = 380;
    private const int PanelHeightSave = 420;
    private const int ListPadding = 6;

    private readonly string _title;
    private readonly FileBrowserMode _mode;
    private readonly string[] _extensions;
    private readonly TextInputField _filenameInput;

    private string _currentDirectory;
    private List<(string Name, bool IsDirectory, string FullPath)> _entries = new();
    private int _selectedIndex;
    private int _scrollOffset;
    private int _hoverIndex = -1;

    // Mouse tracking (IDialog doesn't pass mouse state)
    private MouseState _prevMouse;
    private bool _mouseInitialized;
    private double _lastClickTime;
    private int _lastClickIndex = -1;
    private const double DoubleClickTime = 0.4;

    // Computed layout
    private Rectangle _panelRect;
    private Rectangle _listRect;
    private Rectangle _inputRect;
    private int _itemHeight;
    private int _visibleCount;

    public bool IsComplete { get; private set; }
    public bool WasCancelled { get; private set; }
    public string ResultPath { get; private set; }

    public FileBrowserDialog(string title, string startDir, string[] extensions,
                             FileBrowserMode mode, string defaultFilename = "")
    {
        _title = title;
        _mode = mode;
        _extensions = extensions;

        // Resolve start directory
        if (string.IsNullOrEmpty(startDir) || !Directory.Exists(startDir))
            startDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _currentDirectory = Path.GetFullPath(startDir);

        if (_mode == FileBrowserMode.Save)
        {
            _filenameInput = new TextInputField(defaultFilename, maxLength: 256);
            _filenameInput.IsFocused = true;
        }

        RefreshEntries();
    }

    private void RefreshEntries()
    {
        _entries.Clear();

        // ".." entry unless at root
        var parent = Directory.GetParent(_currentDirectory);
        if (parent != null)
            _entries.Add(("..", true, parent.FullName));

        try
        {
            // Directories first, alphabetically
            var dirs = Directory.GetDirectories(_currentDirectory)
                .Select(d => Path.GetFileName(d))
                .Where(n => !n.StartsWith('.'))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

            foreach (var dir in dirs)
                _entries.Add((dir + "/", true, Path.Combine(_currentDirectory, dir)));

            // Files filtered by extensions, alphabetically
            var files = Directory.GetFiles(_currentDirectory)
                .Select(f => Path.GetFileName(f))
                .Where(n => !n.StartsWith('.') && MatchesExtension(n))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
                _entries.Add((file, false, Path.Combine(_currentDirectory, file)));
        }
        catch
        {
            // Permission denied or other IO error — show what we have
        }

        _selectedIndex = 0;
        _scrollOffset = 0;
    }

    private bool MatchesExtension(string filename)
    {
        if (_extensions == null || _extensions.Length == 0)
            return true;

        foreach (var ext in _extensions)
        {
            if (filename.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public void Update(KeyboardState keyboard, KeyboardState prevKeyboard, GameTime gameTime)
    {
        // Mouse handling
        var mouse = Mouse.GetState();
        if (!_mouseInitialized)
        {
            _prevMouse = mouse;
            _mouseInitialized = true;
        }

        HandleMouse(mouse, gameTime);
        _prevMouse = mouse;

        if (IsComplete) return;

        // Escape
        if (KeyPressed(keyboard, prevKeyboard, Keys.Escape))
        {
            IsComplete = true;
            WasCancelled = true;
            return;
        }

        // In Save mode, route most keys to filename input when focused
        bool inputFocused = _mode == FileBrowserMode.Save && _filenameInput != null && _filenameInput.IsFocused;

        // Tab toggles focus in Save mode
        if (_mode == FileBrowserMode.Save && KeyPressed(keyboard, prevKeyboard, Keys.Tab))
        {
            _filenameInput.IsFocused = !_filenameInput.IsFocused;
            return;
        }

        // Backspace
        if (KeyPressed(keyboard, prevKeyboard, Keys.Back))
        {
            if (inputFocused)
            {
                if (_filenameInput.Text.Length == 0)
                    NavigateToParent();
                else
                    _filenameInput.HandleKey(Keys.Back);
            }
            else
            {
                NavigateToParent();
            }
            return;
        }

        // Forward text editing keys to input in Save mode
        if (inputFocused)
        {
            if (KeyPressed(keyboard, prevKeyboard, Keys.Delete))
                _filenameInput.HandleKey(Keys.Delete);
            if (KeyPressed(keyboard, prevKeyboard, Keys.Left))
                _filenameInput.HandleKey(Keys.Left);
            if (KeyPressed(keyboard, prevKeyboard, Keys.Right))
                _filenameInput.HandleKey(Keys.Right);
            if (KeyPressed(keyboard, prevKeyboard, Keys.Home))
                _filenameInput.HandleKey(Keys.Home);
            if (KeyPressed(keyboard, prevKeyboard, Keys.End))
                _filenameInput.HandleKey(Keys.End);
        }

        // Up/Down navigation (always available, even when input focused)
        if (KeyPressed(keyboard, prevKeyboard, Keys.Up))
        {
            if (_selectedIndex > 0)
            {
                _selectedIndex--;
                EnsureSelectedVisible();
            }
            return;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Down))
        {
            if (_selectedIndex < _entries.Count - 1)
            {
                _selectedIndex++;
                EnsureSelectedVisible();
            }
            return;
        }

        // Enter
        if (KeyPressed(keyboard, prevKeyboard, Keys.Enter))
        {
            ConfirmSelection();
            return;
        }
    }

    private void HandleMouse(MouseState mouse, GameTime gameTime)
    {
        if (_listRect.Width == 0) return; // Layout not computed yet

        _hoverIndex = -1;

        // Scroll wheel
        int scrollDelta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
        if (scrollDelta != 0 && _panelRect.Contains(mouse.X, mouse.Y))
        {
            _scrollOffset -= scrollDelta > 0 ? 3 : -3;
            ClampScroll();
        }

        // Hover detection in list area
        if (_listRect.Contains(mouse.X, mouse.Y))
        {
            int relativeY = mouse.Y - _listRect.Y;
            int index = _scrollOffset + relativeY / _itemHeight;
            if (index >= 0 && index < _entries.Count)
                _hoverIndex = index;
        }

        // Click handling
        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        {
            // Click-to-focus between list and filename input
            if (_mode == FileBrowserMode.Save && _filenameInput != null)
            {
                if (_inputRect.Contains(mouse.X, mouse.Y))
                {
                    _filenameInput.IsFocused = true;
                    return;
                }
                else if (_listRect.Contains(mouse.X, mouse.Y))
                {
                    _filenameInput.IsFocused = false;
                }
            }

            if (_hoverIndex >= 0)
            {
                double now = gameTime.TotalGameTime.TotalSeconds;

                // Double-click detection
                if (_hoverIndex == _lastClickIndex && (now - _lastClickTime) < DoubleClickTime)
                {
                    _selectedIndex = _hoverIndex;
                    var dcEntry = _entries[_selectedIndex];

                    // Double-clicking a directory always navigates, regardless of input focus
                    if (dcEntry.IsDirectory)
                    {
                        if (dcEntry.Name == "..")
                            NavigateToParent();
                        else
                            NavigateToDirectory(dcEntry.FullPath);
                    }
                    else
                    {
                        ConfirmSelection();
                    }

                    _lastClickIndex = -1;
                    return;
                }

                _selectedIndex = _hoverIndex;
                _lastClickTime = now;
                _lastClickIndex = _hoverIndex;

                // In Save mode, clicking updates focus based on what was clicked
                if (_mode == FileBrowserMode.Save && _filenameInput != null)
                {
                    if (_entries[_selectedIndex].IsDirectory)
                    {
                        // Clicking a directory shifts focus to the list
                        // so Enter will navigate instead of save
                        _filenameInput.IsFocused = false;
                    }
                    else
                    {
                        // Clicking a file fills the filename input and focuses it
                        string name = _entries[_selectedIndex].Name;
                        _filenameInput.IsFocused = true;
                        SetFilenameText(name);
                    }
                }
            }
        }
    }

    private void SetFilenameText(string text)
    {
        _filenameInput.SetText(text);
    }

    private void ConfirmSelection()
    {
        // In Save mode, when the filename input is focused and has text, save directly
        if (_mode == FileBrowserMode.Save && _filenameInput != null
            && _filenameInput.IsFocused && _filenameInput.Text.Trim().Length > 0)
        {
            SaveWithFilename();
            return;
        }

        if (_entries.Count == 0) return;

        if (_selectedIndex >= 0 && _selectedIndex < _entries.Count)
        {
            var entry = _entries[_selectedIndex];

            if (entry.IsDirectory)
            {
                // Navigate into directory
                if (entry.Name == "..")
                    NavigateToParent();
                else
                    NavigateToDirectory(entry.FullPath);
                return;
            }

            if (_mode == FileBrowserMode.Open)
            {
                // Confirm file selection
                ResultPath = entry.FullPath;
                IsComplete = true;
                WasCancelled = false;
                return;
            }
        }

        // Save mode fallback: use filename input even when list has focus
        if (_mode == FileBrowserMode.Save && _filenameInput != null)
            SaveWithFilename();
    }

    private void SaveWithFilename()
    {
        string filename = _filenameInput.Text.Trim();
        if (string.IsNullOrEmpty(filename)) return;

        // Auto-append default extension if missing
        if (!Path.HasExtension(filename) && _extensions.Length > 0)
            filename += _extensions[0];

        ResultPath = Path.Combine(_currentDirectory, filename);
        IsComplete = true;
        WasCancelled = false;
    }

    private void NavigateToParent()
    {
        var parent = Directory.GetParent(_currentDirectory);
        if (parent != null)
            NavigateToDirectory(parent.FullName);
    }

    private void NavigateToDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            _currentDirectory = Path.GetFullPath(path);
            RefreshEntries();
        }
    }

    private void EnsureSelectedVisible()
    {
        if (_selectedIndex < _scrollOffset)
            _scrollOffset = _selectedIndex;
        else if (_selectedIndex >= _scrollOffset + _visibleCount)
            _scrollOffset = _selectedIndex - _visibleCount + 1;
        ClampScroll();
    }

    private void ClampScroll()
    {
        int maxScroll = Math.Max(0, _entries.Count - _visibleCount);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
    }

    public void OnTextInput(char character)
    {
        if (_mode == FileBrowserMode.Save && _filenameInput != null && _filenameInput.IsFocused)
            _filenameInput.HandleCharacter(character);
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                     int screenWidth, int screenHeight, GameTime gameTime)
    {
        // Overlay
        renderer.DrawRect(spriteBatch, new Rectangle(0, 0, screenWidth, screenHeight), OverlayColor);

        // Panel
        int panelH = _mode == FileBrowserMode.Save ? PanelHeightSave : PanelHeightOpen;
        int px = (screenWidth - PanelWidth) / 2;
        int py = (screenHeight - panelH) / 2;
        _panelRect = new Rectangle(px, py, PanelWidth, panelH);
        renderer.DrawRect(spriteBatch, _panelRect, PanelColor);
        renderer.DrawRectOutline(spriteBatch, _panelRect, PanelBorder, 1);

        _itemHeight = font.LineSpacing + 4;
        int contentX = px + 10;
        int contentW = PanelWidth - 20;
        int curY = py + 8;

        // Title
        spriteBatch.DrawString(font, _title, new Vector2(contentX, curY), Color.White);
        curY += font.LineSpacing + 4;

        // Current path (truncated from left if too long)
        string pathDisplay = _currentDirectory;
        float maxPathWidth = contentW;
        if (font.MeasureString(pathDisplay).X > maxPathWidth)
        {
            while (pathDisplay.Length > 3 && font.MeasureString("..." + pathDisplay).X > maxPathWidth)
                pathDisplay = pathDisplay[1..];
            pathDisplay = "..." + pathDisplay;
        }
        spriteBatch.DrawString(font, pathDisplay, new Vector2(contentX, curY), PathColor);
        curY += font.LineSpacing + 6;

        // Separator
        renderer.DrawRect(spriteBatch, new Rectangle(px + 1, curY, PanelWidth - 2, 1), SeparatorColor);
        curY += 2;

        // List area
        int listBottom = py + panelH - font.LineSpacing - 16; // leave room for hints
        if (_mode == FileBrowserMode.Save)
            listBottom -= 40; // room for filename input
        int listHeight = listBottom - curY;
        _listRect = new Rectangle(px + 1, curY, PanelWidth - 2, listHeight);
        _visibleCount = listHeight / _itemHeight;
        ClampScroll();

        // Draw entries with scissor clipping
        var prevScissor = spriteBatch.GraphicsDevice.ScissorRectangle;
        var scissorRasterizer = new RasterizerState { ScissorTestEnable = true };
        spriteBatch.End();
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: scissorRasterizer);
        spriteBatch.GraphicsDevice.ScissorRectangle = _listRect;

        int endIndex = Math.Min(_scrollOffset + _visibleCount + 1, _entries.Count);
        for (int i = _scrollOffset; i < endIndex; i++)
        {
            int ey = curY + (i - _scrollOffset) * _itemHeight;
            if (ey + _itemHeight < _listRect.Y || ey > _listRect.Bottom)
                continue;

            var entryRect = new Rectangle(_listRect.X, ey, _listRect.Width, _itemHeight);

            // Selection / hover highlight
            if (i == _selectedIndex)
                renderer.DrawRect(spriteBatch, entryRect, SelectedColor);
            else if (i == _hoverIndex)
                renderer.DrawRect(spriteBatch, entryRect, HoverColor);

            var entry = _entries[i];
            var textColor = entry.IsDirectory ? DirColor : FileColor;
            spriteBatch.DrawString(font, entry.Name,
                new Vector2(contentX, ey + 2), textColor);
        }

        spriteBatch.End();
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        spriteBatch.GraphicsDevice.ScissorRectangle = prevScissor;

        // Scroll bar
        if (_entries.Count > _visibleCount)
        {
            int barX = px + PanelWidth - 10;
            int barH = listHeight;
            renderer.DrawRect(spriteBatch, new Rectangle(barX, _listRect.Y, 6, barH), ScrollBarColor);

            float thumbRatio = (float)_visibleCount / _entries.Count;
            int thumbH = Math.Max(20, (int)(barH * thumbRatio));
            float scrollRatio = _entries.Count <= _visibleCount ? 0 : (float)_scrollOffset / (_entries.Count - _visibleCount);
            int thumbY = _listRect.Y + (int)((barH - thumbH) * scrollRatio);
            renderer.DrawRect(spriteBatch, new Rectangle(barX, thumbY, 6, thumbH), ScrollThumbColor);
        }

        // Separator after list
        int sepY = _listRect.Bottom + 1;
        renderer.DrawRect(spriteBatch, new Rectangle(px + 1, sepY, PanelWidth - 2, 1), SeparatorColor);

        // Save mode: filename input
        if (_mode == FileBrowserMode.Save && _filenameInput != null)
        {
            int inputY = sepY + 6;
            var inputBounds = new Rectangle(contentX, inputY, contentW, 28);
            _inputRect = inputBounds;
            _filenameInput.Draw(spriteBatch, font, renderer, inputBounds, gameTime);
        }

        // Hints
        string hint;
        if (_mode == FileBrowserMode.Save)
            hint = "[Enter] Save   [Tab] Switch focus   [Bksp] Up   [Esc] Cancel";
        else
            hint = "[Enter] Open   [Bksp] Up   [Esc] Cancel";

        var hintSize = font.MeasureString(hint);
        spriteBatch.DrawString(font, hint,
            new Vector2(px + PanelWidth - hintSize.X - 10, py + panelH - font.LineSpacing - 8), HintColor);
    }

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key)
    {
        return current.IsKeyDown(key) && prev.IsKeyUp(key);
    }
}
