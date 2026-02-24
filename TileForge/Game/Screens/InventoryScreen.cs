using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game.Screens;

/// <summary>
/// Overlay screen showing the player's inventory. Items are grouped by name
/// with a count. The player can use consumable items (those with a "heal"
/// property) or close the screen with Cancel / OpenInventory toggle.
/// </summary>
public class InventoryScreen : GameScreen
{
    private readonly GameStateManager _gameStateManager;
    private int _selectedIndex;
    private string _statusMessage;
    private float _statusTimer;

    public override bool IsOverlay => true;

    public InventoryScreen(GameStateManager gameStateManager)
    {
        _gameStateManager = gameStateManager;
    }

    /// <summary>
    /// Groups inventory items by name, returning (name, count) pairs.
    /// </summary>
    private List<(string Name, int Count)> GetGroupedItems()
    {
        return _gameStateManager.State.Player.Inventory
            .GroupBy(name => name)
            .Select(g => (Name: g.Key, Count: g.Count()))
            .ToList();
    }

    // Menu: grouped items + "Close"
    private int MenuItemCount(List<(string Name, int Count)> items) => items.Count + 1;
    private bool IsCloseItem(List<(string Name, int Count)> items, int index) => index == items.Count;

    public override void Update(GameTime gameTime, GameInputManager input)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Tick status message
        if (_statusTimer > 0)
        {
            _statusTimer -= dt;
            if (_statusTimer <= 0)
                _statusMessage = null;
        }

        var items = GetGroupedItems();
        int count = MenuItemCount(items);

        if (input.IsActionJustPressed(GameAction.MoveUp))
        {
            _selectedIndex--;
            if (_selectedIndex < 0) _selectedIndex = count - 1;
        }
        if (input.IsActionJustPressed(GameAction.MoveDown))
        {
            _selectedIndex++;
            if (_selectedIndex >= count) _selectedIndex = 0;
        }

        // Clamp in case inventory changed
        if (_selectedIndex >= count)
            _selectedIndex = count > 0 ? count - 1 : 0;

        if (input.IsActionJustPressed(GameAction.Interact))
        {
            if (IsCloseItem(items, _selectedIndex))
            {
                ScreenManager.Pop();
                return;
            }

            if (_selectedIndex < items.Count)
            {
                var item = items[_selectedIndex];
                UseItem(item.Name);
            }
            return;
        }

        // Cancel or toggle inventory closes
        if (input.IsActionJustPressed(GameAction.Cancel) ||
            input.IsActionJustPressed(GameAction.OpenInventory))
        {
            ScreenManager.Pop();
        }
    }

    private void UseItem(string itemName)
    {
        // Look up item properties from the cache (populated at collection time)
        int healAmount = 0;
        if (_gameStateManager.State.ItemPropertyCache.TryGetValue(itemName, out var props)
            && props.TryGetValue("heal", out var healStr))
        {
            int.TryParse(healStr, out healAmount);
        }

        if (healAmount > 0 && _gameStateManager.State.Player.Health < _gameStateManager.State.Player.MaxHealth)
        {
            _gameStateManager.HealPlayer(healAmount);
            _gameStateManager.RemoveFromInventory(itemName);
            _statusMessage = $"Used {itemName}: healed {healAmount} HP";
            _statusTimer = 2.0f;
        }
        else if (healAmount > 0)
        {
            _statusMessage = "Already at full health";
            _statusTimer = 2.0f;
        }
        else
        {
            _statusMessage = $"Cannot use {itemName}";
            _statusTimer = 2.0f;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, SpriteFont font,
        Renderer renderer, Rectangle canvasBounds)
    {
        // Dark overlay
        renderer.DrawRect(spriteBatch, canvasBounds, new Color(0, 0, 0, 180));

        // Title
        var titleText = "INVENTORY";
        var titleSize = font.MeasureString(titleText);
        var titlePos = new Vector2(
            canvasBounds.X + (canvasBounds.Width - titleSize.X) / 2f,
            canvasBounds.Y + 40f);
        spriteBatch.DrawString(font, titleText, titlePos, Color.White);

        // Items
        var items = GetGroupedItems();
        int count = MenuItemCount(items);
        float startY = titlePos.Y + titleSize.Y + 20f;

        if (items.Count == 0)
        {
            var emptyText = "No items";
            var emptySize = font.MeasureString(emptyText);
            var emptyPos = new Vector2(
                canvasBounds.X + (canvasBounds.Width - emptySize.X) / 2f,
                startY);
            spriteBatch.DrawString(font, emptyText, emptyPos, Color.Gray);
            startY += emptySize.Y + 8f;
        }

        for (int i = 0; i < count; i++)
        {
            string text;
            if (i < items.Count)
            {
                var item = items[i];
                text = item.Count > 1 ? $"{item.Name} x{item.Count}" : item.Name;
            }
            else
            {
                text = "Close";
            }

            var itemSize = font.MeasureString(text);
            var itemPos = new Vector2(
                canvasBounds.X + (canvasBounds.Width - itemSize.X) / 2f,
                startY + i * (itemSize.Y + 6f));
            var color = i == _selectedIndex ? Color.Yellow : Color.White;
            spriteBatch.DrawString(font, text, itemPos, color);
        }

        // Status message
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            var msgSize = font.MeasureString(_statusMessage);
            var msgPos = new Vector2(
                canvasBounds.X + (canvasBounds.Width - msgSize.X) / 2f,
                canvasBounds.Y + canvasBounds.Height * 0.85f);
            spriteBatch.DrawString(font, _statusMessage, msgPos, Color.LimeGreen);
        }
    }
}
