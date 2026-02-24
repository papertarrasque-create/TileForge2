using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

public record MenuItemDef(string Label, string Hotkey = null, bool Enabled = true, bool IsSeparator = false)
{
    public static MenuItemDef Separator => new("", IsSeparator: true);
}

public record MenuDef(string Label, MenuItemDef[] Items);

public class MenuBar
{
    public const int Height = 22;

    private static readonly Color BarBackground   = new(40, 40, 40);
    private static readonly Color BarSeparator    = new(60, 60, 60);
    private static readonly Color LabelText       = new(200, 200, 200);
    private static readonly Color LabelHover      = new(55, 55, 55);
    private static readonly Color LabelActive     = new(60, 60, 60);
    private static readonly Color PopupBackground = new(40, 40, 40);
    private static readonly Color PopupBorder     = new(80, 80, 80);
    private static readonly Color ItemText        = new(200, 200, 200);
    private static readonly Color ItemDisabled    = new(80, 80, 80);
    private static readonly Color ItemHover       = new(60, 70, 90);
    private static readonly Color HotkeyText      = new(120, 120, 120);
    private static readonly Color SeparatorLine   = new(60, 60, 60);

    private const int LabelPadX     = 8;   // padding from left edge before first label
    private const int LabelPadEach  = 12;  // horizontal padding each side of a label
    private const int SeparatorH    = 7;
    private const int SeparatorMarginX = 4;
    private const int MinPopupWidth = 180;
    private const int HotkeyRightPad = 8;

    private readonly MenuDef[] _menus;
    private readonly bool[,]   _itemEnabled;

    private int         _openMenuIndex    = -1;
    private int         _hoveredItemIndex = -1;
    private Rectangle[] _labelRects       = Array.Empty<Rectangle>();
    private Rectangle   _submenuRect;

    public bool      IsMenuOpen => _openMenuIndex >= 0;
    public MenuDef[] Menus      => _menus;

    public MenuBar(MenuDef[] menus)
    {
        _menus = menus ?? throw new ArgumentNullException(nameof(menus));

        int maxItems = 0;
        foreach (var m in _menus)
            if (m.Items != null && m.Items.Length > maxItems)
                maxItems = m.Items.Length;

        _itemEnabled = new bool[_menus.Length, Math.Max(maxItems, 1)];

        for (int mi = 0; mi < _menus.Length; mi++)
        {
            if (_menus[mi].Items == null) continue;
            for (int ii = 0; ii < _menus[mi].Items.Length; ii++)
                _itemEnabled[mi, ii] = _menus[mi].Items[ii].Enabled;
        }

        _labelRects = new Rectangle[_menus.Length];
    }

    public void SetItemEnabled(int menuIndex, int itemIndex, bool enabled)
    {
        if (menuIndex < 0 || menuIndex >= _menus.Length) return;
        if (itemIndex < 0 || itemIndex >= _menus[menuIndex].Items.Length) return;
        _itemEnabled[menuIndex, itemIndex] = enabled;
    }

    /// <summary>
    /// Returns (menuIndex, itemIndex) of a clicked enabled submenu item,
    /// or (-1, -1) if no action this frame.
    /// </summary>
    public (int Menu, int Item) Update(MouseState mouse, MouseState prevMouse, int screenWidth)
    {
        bool clicked = mouse.LeftButton == ButtonState.Pressed &&
                       prevMouse.LeftButton == ButtonState.Released;

        int mx = mouse.X;
        int my = mouse.Y;

        // --- Hover-to-switch: when a menu is open, hovering another top-level label switches it ---
        if (_openMenuIndex >= 0)
        {
            for (int i = 0; i < _labelRects.Length; i++)
            {
                if (_labelRects[i].Contains(mx, my) && i != _openMenuIndex)
                {
                    _openMenuIndex    = i;
                    _hoveredItemIndex = -1;
                    break;
                }
            }
        }

        // --- Top-level bar click ---
        bool onBar = my >= 0 && my < Height;

        if (onBar)
        {
            for (int i = 0; i < _labelRects.Length; i++)
            {
                if (_labelRects[i].Contains(mx, my))
                {
                    if (clicked)
                    {
                        if (_openMenuIndex == i)
                        {
                            // Already open — close it
                            _openMenuIndex    = -1;
                            _hoveredItemIndex = -1;
                        }
                        else
                        {
                            _openMenuIndex    = i;
                            _hoveredItemIndex = -1;
                        }
                    }
                    return (-1, -1);
                }
            }
        }

        // --- Submenu interaction ---
        if (_openMenuIndex >= 0)
        {
            bool onSubmenu = _submenuRect.Contains(mx, my);

            if (onSubmenu)
            {
                // Compute hovered item
                var items   = _menus[_openMenuIndex].Items;
                int itemH   = GetItemHeight(null);    // approximate without font
                int relY    = my - _submenuRect.Y;
                int cumY    = 0;
                int hovIdx  = -1;

                for (int ii = 0; ii < items.Length; ii++)
                {
                    int h = items[ii].IsSeparator ? SeparatorH : itemH;
                    if (relY >= cumY && relY < cumY + h)
                    {
                        hovIdx = ii;
                        break;
                    }
                    cumY += h;
                }

                _hoveredItemIndex = hovIdx;

                if (clicked && hovIdx >= 0)
                {
                    var item = items[hovIdx];
                    if (!item.IsSeparator && _itemEnabled[_openMenuIndex, hovIdx])
                    {
                        int mi = _openMenuIndex;
                        _openMenuIndex    = -1;
                        _hoveredItemIndex = -1;
                        return (mi, hovIdx);
                    }
                }
            }
            else
            {
                _hoveredItemIndex = -1;

                // Click outside both bar and submenu — close
                if (clicked)
                {
                    _openMenuIndex = -1;
                }
            }
        }

        return (-1, -1);
    }

    // Fallback item height (no font available in Update). Draw recalculates with real font.
    private static int GetItemHeight(SpriteFont font)
    {
        // MonoGame default LineSpacing for a typical small font is ~16
        // We use this only in Update where font is not available.
        // Draw recomputes correctly using the real font.
        return 16 + 6;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer, int screenWidth)
    {
        // Bar background
        renderer.DrawRect(spriteBatch, new Rectangle(0, 0, screenWidth, Height), BarBackground);

        // Bottom 1px separator
        renderer.DrawRect(spriteBatch, new Rectangle(0, Height - 1, screenWidth, 1), BarSeparator);

        // Top-level labels
        int x = LabelPadX;
        for (int i = 0; i < _menus.Length; i++)
        {
            string label    = _menus[i].Label;
            var    textSize = font.MeasureString(label);
            int    w        = (int)textSize.X + LabelPadEach * 2;
            var    rect     = new Rectangle(x, 0, w, Height);
            _labelRects[i] = rect;

            // Highlight
            bool isOpen   = _openMenuIndex == i;
            bool isHovered = rect.Contains(new Point(0, 0)); // updated below

            if (isOpen)
                renderer.DrawRect(spriteBatch, rect, LabelActive);

            // Text centered vertically in 22px bar
            float textY = (Height - textSize.Y) / 2f;
            spriteBatch.DrawString(font, label,
                new Vector2(x + LabelPadEach, textY), LabelText);

            x += w;
        }
    }

    public void DrawSubmenu(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer)
    {
        if (_openMenuIndex < 0 || _openMenuIndex >= _menus.Length) return;

        var menu  = _menus[_openMenuIndex];
        var items = menu.Items;
        if (items == null || items.Length == 0) return;

        int itemH = font.LineSpacing + 6;

        // Measure popup width
        float maxLabelW  = 0;
        float maxHotkeyW = 0;
        foreach (var item in items)
        {
            if (item.IsSeparator) continue;
            var lw = font.MeasureString(item.Label).X;
            if (lw > maxLabelW) maxLabelW = lw;
            if (item.Hotkey != null)
            {
                var hw = font.MeasureString(item.Hotkey).X;
                if (hw > maxHotkeyW) maxHotkeyW = hw;
            }
        }

        int gapBetween = maxHotkeyW > 0 ? (int)maxHotkeyW + HotkeyRightPad + 16 : 0;
        int popupWidth = Math.Max(MinPopupWidth,
            (int)maxLabelW + gapBetween + LabelPadEach * 2);

        // Total height
        int totalH = 0;
        foreach (var item in items)
            totalH += item.IsSeparator ? SeparatorH : itemH;

        // Position: below the open label rect
        int popupX = _labelRects[_openMenuIndex].X;
        int popupY = Height;

        _submenuRect = new Rectangle(popupX, popupY, popupWidth, totalH);

        // Background + border
        renderer.DrawRect(spriteBatch, _submenuRect, PopupBackground);
        renderer.DrawRectOutline(spriteBatch, _submenuRect, PopupBorder, 1);

        // Items
        int iy = popupY;
        for (int ii = 0; ii < items.Length; ii++)
        {
            var item = items[ii];

            if (item.IsSeparator)
            {
                int lineY = iy + SeparatorH / 2;
                renderer.DrawRect(spriteBatch,
                    new Rectangle(popupX + SeparatorMarginX, lineY,
                                  popupWidth - SeparatorMarginX * 2, 1),
                    SeparatorLine);
                iy += SeparatorH;
                continue;
            }

            bool isHovered  = ii == _hoveredItemIndex;
            bool isEnabled  = _itemEnabled[_openMenuIndex, ii];

            if (isHovered && isEnabled)
            {
                renderer.DrawRect(spriteBatch,
                    new Rectangle(popupX + 1, iy, popupWidth - 2, itemH),
                    ItemHover);
            }

            Color textColor = isEnabled ? ItemText : ItemDisabled;
            float textY = iy + 3f;

            spriteBatch.DrawString(font, item.Label,
                new Vector2(popupX + LabelPadEach, textY), textColor);

            if (item.Hotkey != null)
            {
                var hkSize = font.MeasureString(item.Hotkey);
                float hkX  = popupX + popupWidth - hkSize.X - HotkeyRightPad;
                spriteBatch.DrawString(font, item.Hotkey,
                    new Vector2(hkX, textY), HotkeyText);
            }

            iy += itemH;
        }
    }
}
