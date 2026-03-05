using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game.Screens;

/// <summary>
/// Screen overlay for saving and loading game state. Displayed from the PauseScreen.
/// In Save mode: lists existing slots plus a "New Save" option.
/// In Load mode: lists existing slots only.
/// Both modes have a "Back" item at the bottom.
/// </summary>
public enum SaveLoadMode { Save, Load }

public class SaveLoadScreen : GameScreen
{
    private readonly SaveManager _saveManager;
    private readonly GameStateManager _gameStateManager;
    private readonly SaveLoadMode _mode;
    private List<string> _slots;
    private GameMenuList _menu;
    private string _statusMessage;
    private float _statusTimer;

    public override bool IsOverlay => true;

    public SaveLoadScreen(SaveManager saveManager, GameStateManager gameStateManager, SaveLoadMode mode)
    {
        _saveManager = saveManager;
        _gameStateManager = gameStateManager;
        _mode = mode;
        _slots = new List<string>();
    }

    public override void OnEnter()
    {
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        _slots = _saveManager.GetSlots();
    }

    /// <summary>
    /// Total number of menu entries: slots + "New Save" (save mode only) + "Back".
    /// </summary>
    private int MenuItemCount
    {
        get
        {
            int count = _slots.Count;
            if (_mode == SaveLoadMode.Save) count++; // "New Save"
            count++; // "Back"
            return count;
        }
    }

    private string GetMenuItemText(int index)
    {
        if (index < _slots.Count)
            return _slots[index];

        int offset = index - _slots.Count;
        if (_mode == SaveLoadMode.Save && offset == 0)
            return "New Save";

        return "Back";
    }

    private bool IsBackItem(int index) => index == MenuItemCount - 1;

    private bool IsNewSaveItem(int index) =>
        _mode == SaveLoadMode.Save && index == _slots.Count;

    public override void Update(GameTime gameTime, GameInputManager input)
    {
        // Tick status message timer
        if (_statusTimer > 0)
        {
            _statusTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_statusTimer <= 0)
                _statusMessage = null;
        }

        if (input.IsActionJustPressed(GameAction.MoveUp))
            _menu.MoveUp(MenuItemCount);

        if (input.IsActionJustPressed(GameAction.MoveDown))
            _menu.MoveDown(MenuItemCount);

        if (input.IsActionJustPressed(GameAction.Interact))
        {
            if (IsBackItem(_menu.SelectedIndex))
            {
                ScreenManager.Pop();
                return;
            }

            if (IsNewSaveItem(_menu.SelectedIndex))
            {
                // Auto-generate a slot name based on existing count
                string slotName = "save_" + (_slots.Count + 1).ToString("D2");
                _saveManager.Save(_gameStateManager.State, slotName);
                _statusMessage = $"Saved to {slotName}";
                _statusTimer = 2.0f;
                RefreshSlots();
                return;
            }

            // Existing slot selected
            string selectedSlot = _slots[_menu.SelectedIndex];
            if (_mode == SaveLoadMode.Save)
            {
                _saveManager.Save(_gameStateManager.State, selectedSlot);
                _statusMessage = $"Saved to {selectedSlot}";
                _statusTimer = 2.0f;
            }
            else
            {
                var loaded = _saveManager.Load(selectedSlot);
                _gameStateManager.LoadState(loaded);
                _statusMessage = $"Loaded {selectedSlot}";
                _statusTimer = 2.0f;
                ScreenManager.Pop(); // Return to gameplay
            }
            return;
        }

        if (input.IsActionJustPressed(GameAction.Cancel))
        {
            ScreenManager.Pop();
        }
    }

    public override void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer, Rectangle canvasBounds)
    {
        // Dark overlay
        renderer.DrawRect(spriteBatch, canvasBounds, new Color(0, 0, 0, 180));

        // Title
        string title = _mode == SaveLoadMode.Save ? "SAVE GAME" : "LOAD GAME";
        var titleSize = font.MeasureString(title);
        var titlePos = new Vector2(
            canvasBounds.X + (canvasBounds.Width - titleSize.X) / 2f,
            canvasBounds.Y + canvasBounds.Height / 4f);
        spriteBatch.DrawString(font, title, titlePos, Color.White);

        // Menu items
        float menuStartY = titlePos.Y + titleSize.Y + 20f;
        int itemCount = MenuItemCount;
        for (int i = 0; i < itemCount; i++)
        {
            string text = GetMenuItemText(i);
            var itemSize = font.MeasureString(text);
            var itemPos = new Vector2(
                canvasBounds.X + (canvasBounds.Width - itemSize.X) / 2f,
                menuStartY + i * (itemSize.Y + 8f));
            var color = i == _menu.SelectedIndex ? Color.Yellow : Color.White;
            spriteBatch.DrawString(font, text, itemPos, color);
        }

        // Status message near bottom
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            var msgSize = font.MeasureString(_statusMessage);
            var msgPos = new Vector2(
                canvasBounds.X + (canvasBounds.Width - msgSize.X) / 2f,
                canvasBounds.Y + canvasBounds.Height * 0.8f);
            spriteBatch.DrawString(font, _statusMessage, msgPos, Color.LimeGreen);
        }
    }
}
