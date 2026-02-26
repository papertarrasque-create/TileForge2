using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game.Screens;

/// <summary>
/// Overlay screen showing the player's equipment and inventory. The menu has
/// three sections: equipment slots, inventory items (grouped by name with count),
/// and a Close button. The player can equip/unequip gear, use consumable items
/// (those with a "heal" property), or close the screen with Cancel / OpenInventory toggle.
/// </summary>
public class InventoryScreen : GameScreen
{
    private readonly GameStateManager _gameStateManager;
    private GameMenuList _menu;
    private List<MenuItem> _cachedMenuItems;
    private string _statusMessage;
    private float _statusTimer;

    public override bool IsOverlay => true;

    public InventoryScreen(GameStateManager gameStateManager)
    {
        _gameStateManager = gameStateManager;
    }

    private enum MenuSection { EquipSlot, InventoryItem, Close }

    private record struct MenuItem(MenuSection Section, string Label, string ItemName, EquipmentSlot? Slot);

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

    private List<MenuItem> BuildMenuItems()
    {
        var items = new List<MenuItem>();

        // Equipment slots
        foreach (var slot in Enum.GetValues<EquipmentSlot>())
        {
            string equipped = _gameStateManager.GetEquippedItem(slot);
            string label = equipped != null
                ? $"[{slot}] {equipped}"
                : $"[{slot}] (empty)";
            items.Add(new MenuItem(MenuSection.EquipSlot, label, equipped, slot));
        }

        // Inventory items (grouped)
        var grouped = GetGroupedItems();
        foreach (var group in grouped)
        {
            string label = group.Count > 1 ? $"{group.Name} x{group.Count}" : group.Name;
            if (_gameStateManager.IsEquipped(group.Name))
                label += " [E]";
            items.Add(new MenuItem(MenuSection.InventoryItem, label, group.Name, null));
        }

        // Close
        items.Add(new MenuItem(MenuSection.Close, "Close", null, null));

        return items;
    }

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

        _cachedMenuItems = BuildMenuItems();
        var menuItems = _cachedMenuItems;
        int count = menuItems.Count;

        if (input.IsActionJustPressed(GameAction.MoveUp))
            _menu.MoveUp(count);
        if (input.IsActionJustPressed(GameAction.MoveDown))
            _menu.MoveDown(count);

        // Clamp in case inventory changed
        _menu.ClampIndex(count);

        if (input.IsActionJustPressed(GameAction.Interact))
        {
            if (_menu.SelectedIndex < count)
            {
                var item = menuItems[_menu.SelectedIndex];
                switch (item.Section)
                {
                    case MenuSection.Close:
                        ScreenManager.Pop();
                        return;

                    case MenuSection.EquipSlot:
                        if (item.ItemName != null && item.Slot.HasValue)
                        {
                            _gameStateManager.UnequipItem(item.Slot.Value);
                            _statusMessage = $"Unequipped {item.ItemName}";
                            _statusTimer = 2.0f;
                        }
                        else
                        {
                            _statusMessage = "Nothing equipped";
                            _statusTimer = 2.0f;
                        }
                        break;

                    case MenuSection.InventoryItem:
                        HandleInventoryInteract(item.ItemName);
                        break;
                }
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

    private void HandleInventoryInteract(string itemName)
    {
        // Check if item is equippable
        var equipSlot = _gameStateManager.GetItemEquipSlot(itemName);
        if (equipSlot.HasValue)
        {
            _gameStateManager.EquipItem(itemName, equipSlot.Value);
            _statusMessage = $"Equipped {itemName} ({equipSlot.Value})";
            _statusTimer = 2.0f;
            return;
        }

        // Fall through to existing heal logic
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

        // Menu items (cached from last Update)
        var menuItems = _cachedMenuItems ?? BuildMenuItems();
        float startY = titlePos.Y + titleSize.Y + 20f;

        // Check if inventory section is empty (only equip slots + close, no actual items)
        bool hasInventoryItems = menuItems.Exists(m => m.Section == MenuSection.InventoryItem);

        for (int i = 0; i < menuItems.Count; i++)
        {
            var item = menuItems[i];
            string text = item.Label;

            var itemSize = font.MeasureString(text);
            var itemPos = new Vector2(
                canvasBounds.X + (canvasBounds.Width - itemSize.X) / 2f,
                startY + i * (itemSize.Y + 6f));

            Color color;
            if (i == _menu.SelectedIndex)
                color = Color.Yellow;
            else if (item.Section == MenuSection.EquipSlot && item.ItemName != null)
                color = Color.Cyan;
            else if (item.Section == MenuSection.EquipSlot)
                color = Color.Gray;
            else
                color = Color.White;

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
