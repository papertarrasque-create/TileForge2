using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;

namespace TileForge.Game.Screens;

/// <summary>
/// Settings screen overlay showing all GameAction key bindings.
/// The user can navigate the list, select an action to rebind it, reset all
/// bindings to defaults, or go back. While waiting for a key press the screen
/// reads the keyboard directly so it can capture any key (including ones not
/// mapped to any GameAction).
/// </summary>
public class SettingsScreen : GameScreen
{
    private readonly GameInputManager _inputManager;
    private readonly string _bindingsPath;
    private readonly GameAction[] _actions;
    private GameMenuList _menu;
    private bool _waitingForKey;
    private KeyboardState _previousKeyState;

    public override bool IsOverlay => true;

    public SettingsScreen(GameInputManager inputManager, string bindingsPath)
    {
        _inputManager = inputManager;
        _bindingsPath = bindingsPath;
        _actions = (GameAction[])Enum.GetValues(typeof(GameAction));
    }

    // Total items: one per action + "Reset Defaults" + "Back"
    private int MenuItemCount => _actions.Length + 2;
    private bool IsResetItem(int index) => index == _actions.Length;
    private bool IsBackItem(int index) => index == _actions.Length + 1;

    public override void Update(GameTime gameTime, GameInputManager input)
    {
        var currentKeyState = Keyboard.GetState();

        if (_waitingForKey)
        {
            // Capture the next newly-pressed key
            foreach (var key in currentKeyState.GetPressedKeys())
            {
                if (!_previousKeyState.IsKeyDown(key) && key != Keys.None)
                {
                    // Escape cancels the rebind without changing anything
                    if (key == Keys.Escape)
                    {
                        _waitingForKey = false;
                        _previousKeyState = currentKeyState;
                        return;
                    }

                    // Apply the new binding and persist it immediately
                    _inputManager.RebindAction(_actions[_menu.SelectedIndex], key);
                    _inputManager.SaveBindings(_bindingsPath);
                    _waitingForKey = false;
                    _previousKeyState = currentKeyState;
                    return;
                }
            }

            _previousKeyState = currentKeyState;
            return;
        }

        // Normal menu navigation via GameInputManager (abstract action layer)
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
            if (IsResetItem(_menu.SelectedIndex))
            {
                _inputManager.ResetDefaults();
                _inputManager.SaveBindings(_bindingsPath);
                return;
            }
            // Enter key-capture mode for the selected action
            _waitingForKey = true;
            _previousKeyState = currentKeyState;
            return;
        }

        if (input.IsActionJustPressed(GameAction.Cancel))
        {
            ScreenManager.Pop();
        }

        _previousKeyState = currentKeyState;
    }

    public override void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer, Rectangle canvasBounds)
    {
        // Semi-transparent dark overlay
        renderer.DrawRect(spriteBatch, canvasBounds, new Color(0, 0, 0, 180));

        // Title — centred horizontally near the top
        var titleText = "SETTINGS";
        var titleSize = font.MeasureString(titleText);
        var titlePos = new Vector2(
            canvasBounds.X + (canvasBounds.Width - titleSize.X) / 2f,
            canvasBounds.Y + 40f);
        spriteBatch.DrawString(font, titleText, titlePos, Color.White);

        // Binding rows
        var bindings = _inputManager.GetBindings();
        float startY = titlePos.Y + titleSize.Y + 20f;

        for (int i = 0; i < MenuItemCount; i++)
        {
            string text;
            if (i < _actions.Length)
            {
                var action = _actions[i];
                string keysText = bindings.TryGetValue(action, out var keys)
                    ? string.Join(", ", keys.Select(k => k.ToString()))
                    : "???";

                text = (_waitingForKey && i == _menu.SelectedIndex)
                    ? $"{action}: [Press a key...]"
                    : $"{action}: {keysText}";
            }
            else if (IsResetItem(i))
            {
                text = "Reset Defaults";
            }
            else
            {
                text = "Back";
            }

            var itemSize = font.MeasureString(text);
            var itemPos = new Vector2(
                canvasBounds.X + (canvasBounds.Width - itemSize.X) / 2f,
                startY + i * (itemSize.Y + 6f));
            var color = i == _menu.SelectedIndex ? Color.Yellow : Color.White;
            spriteBatch.DrawString(font, text, itemPos, color);
        }
    }
}
