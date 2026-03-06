using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge.Play;

namespace TileForge.Game;

/// <summary>
/// Retro CRPG-style sidebar HUD drawn on the right side of the screen
/// during play mode. Shows player stats, equipment, inventory, and a
/// scrollable message log. Inspired by Caves of Qud, SKALD, Ultima, etc.
/// </summary>
public class SidebarHUD
{
    private readonly GameStateManager _gsm;
    private readonly GameLog _log;
    private readonly Func<PlayState> _getPlayState;

    // Log scroll state
    private int _logScrollOffset;
    private int _lastLogVersion = -1;
    private bool _autoScroll = true;

    // Cached stat strings to avoid per-frame allocation
    private int _cachedHP = -1, _cachedMaxHP = -1;
    private int _cachedPoise = -1, _cachedMaxPoise = -1;
    private int _cachedATK = -1, _cachedDEF = -1;
    private int _cachedAP = -1, _cachedMaxAP = -1;
    private string _hpText = "";
    private string _poiseText = "";
    private string _statsText = "";
    private string _apText = "";

    public SidebarHUD(GameStateManager gsm, GameLog log, Func<PlayState> getPlayState)
    {
        _gsm = gsm;
        _log = log;
        _getPlayState = getPlayState;
    }

    // Cached log content width for scroll offset calculations (set during Draw)
    private int _lastContentW;
    private SpriteFont _lastFont;

    /// <summary>
    /// After Draw(), this contains the rectangle where the minimap should be rendered.
    /// The caller is responsible for drawing the minimap into this rect.
    /// </summary>
    public Rectangle MinimapRect { get; private set; }

    /// <summary>
    /// Map dimensions used to compute the adaptive minimap height.
    /// Set by the caller before Draw() when map data is available.
    /// </summary>
    public int MapWidth { get; set; }
    public int MapHeight { get; set; }

    public void Update()
    {
        // Auto-scroll: just track that new messages arrived (Draw handles the rest)
        if (_log.Version != _lastLogVersion)
        {
            _lastLogVersion = _log.Version;
        }

        // Update cached stat strings
        var player = _gsm.State?.Player;
        if (player == null) return;

        int hp = player.Health, maxHP = player.MaxHealth;
        if (hp != _cachedHP || maxHP != _cachedMaxHP)
        {
            _cachedHP = hp; _cachedMaxHP = maxHP;
            _hpText = $"{hp}/{maxHP}";
        }

        int poise = player.Poise, maxPoise = _gsm.GetEffectiveMaxPoise();
        if (poise != _cachedPoise || maxPoise != _cachedMaxPoise)
        {
            _cachedPoise = poise; _cachedMaxPoise = maxPoise;
            _poiseText = $"{poise}/{maxPoise}";
        }

        int atk = _gsm.GetEffectiveAttack(), def = _gsm.GetEffectiveDefense();
        if (atk != _cachedATK || def != _cachedDEF)
        {
            _cachedATK = atk; _cachedDEF = def;
            _statsText = $"ATK {atk}  DEF {def}";
        }

        var play = _getPlayState();
        int ap = play?.PlayerAP ?? 0, maxAP = _gsm.GetEffectiveMaxAP();
        if (ap != _cachedAP || maxAP != _cachedMaxAP)
        {
            _cachedAP = ap; _cachedMaxAP = maxAP;
            var sb = new System.Text.StringBuilder(3 + maxAP);
            for (int i = 0; i < maxAP; i++)
                sb.Append(i < ap ? '*' : '.');
            _apText = sb.ToString();
        }
    }

    /// <summary>
    /// Scroll the log by the given number of entries (negative = up, positive = down).
    /// Scrolling up disables auto-scroll; scrolling to or past the end re-enables it.
    /// </summary>
    public void ScrollLog(int delta)
    {
        if (delta < 0)
        {
            // Scrolling up — disable auto-scroll, clamp offset
            _autoScroll = false;
            _logScrollOffset = Math.Clamp(_logScrollOffset + delta, 0, Math.Max(0, _log.Count - 1));
        }
        else
        {
            // Scrolling down — advance offset; if we'd go past the end, re-enable auto-scroll
            _logScrollOffset += delta;
            if (_logScrollOffset >= _log.Count - 1)
            {
                _autoScroll = true;
                _logScrollOffset = Math.Max(0, _log.Count - 1);
            }
        }
    }

    /// <summary>
    /// Jump to the latest messages.
    /// </summary>
    public void ScrollToBottom()
    {
        _autoScroll = true;
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Renderer renderer, Rectangle sidebarBounds)
    {
        int pad = LayoutConstants.SidebarPadding;
        int x = sidebarBounds.X;
        int y = sidebarBounds.Y;
        int w = sidebarBounds.Width;
        int lineH = (int)font.MeasureString("A").Y;

        // Background
        renderer.DrawRect(sb, sidebarBounds, LayoutConstants.SidebarBackground);

        // Left border line
        renderer.DrawRect(sb, new Rectangle(x, y, 2, sidebarBounds.Height), LayoutConstants.SidebarBorder);

        int cx = x + pad + 2; // content x (after border)
        int cy = y + pad;     // content y cursor
        int contentW = w - pad * 2 - 2;

        // --- PLAYER SECTION ---
        cy = DrawSectionHeader(sb, font, renderer, "-- PLAYER --", cx, cy, contentW);

        // Map name
        string mapName = _gsm.State?.CurrentMapId ?? "Unknown";
        sb.DrawString(font, TextUtils.TruncateToFit(font, mapName, contentW),
            new Vector2(cx, cy), LayoutConstants.SidebarDimTextColor);
        cy += lineH + 2;

        // Health bar
        cy = DrawStatBar(sb, font, renderer, "HP", _hpText, cx, cy, contentW,
            _cachedHP, _cachedMaxHP,
            LayoutConstants.SidebarHealthBarBg,
            _cachedHP > _cachedMaxHP / 2 ? LayoutConstants.SidebarHealthBarFullFill : LayoutConstants.SidebarHealthBarFill);

        // Poise bar
        cy = DrawStatBar(sb, font, renderer, "PP", _poiseText, cx, cy, contentW,
            _cachedPoise, _cachedMaxPoise,
            LayoutConstants.SidebarPoiseBarBg,
            _cachedPoise > _cachedMaxPoise / 2 ? LayoutConstants.SidebarPoiseBarFill : LayoutConstants.SidebarPoiseBarLowFill);

        // ATK/DEF
        sb.DrawString(font, _statsText, new Vector2(cx, cy), LayoutConstants.SidebarTextColor);
        cy += lineH + 2;

        // AP pips
        string apLabel = "AP " + _apText;
        Color apColor = _cachedAP == _cachedMaxAP ? Color.Gold : new Color(160, 140, 100);
        sb.DrawString(font, apLabel, new Vector2(cx, cy), apColor);
        cy += lineH + 2;

        // Status effects
        var effects = _gsm.State?.Player?.ActiveEffects;
        if (effects != null && effects.Count > 0)
        {
            foreach (var effect in effects)
            {
                string label = FormatEffect(effect);
                Color color = EffectColor(effect);
                cy = DrawWrappedText(sb, font, label, cx, cy, contentW, lineH, color);
            }
            cy += 2;
        }

        // Separator
        cy = DrawSeparator(renderer, sb, cx, cy, contentW);

        // --- EQUIPMENT SECTION ---
        cy = DrawSectionHeader(sb, font, renderer, "-- EQUIPMENT --", cx, cy, contentW);
        cy = DrawEquipment(sb, font, cx, cy, contentW, lineH);

        // Separator
        cy = DrawSeparator(renderer, sb, cx, cy, contentW);

        // --- INVENTORY SECTION ---
        cy = DrawSectionHeader(sb, font, renderer, "-- ITEMS --", cx, cy, contentW);
        cy = DrawInventory(sb, font, cx, cy, contentW, lineH);

        // Separator
        cy = DrawSeparator(renderer, sb, cx, cy, contentW);

        // --- Adaptive layout: split remaining space between LOG and MINIMAP ---
        int separatorCost = LayoutConstants.SidebarSeparatorHeight + LayoutConstants.SidebarSectionGap;
        int logHeaderH = lineH + LayoutConstants.SidebarSectionGap;
        int remainingH = sidebarBounds.Bottom - pad - cy - logHeaderH - separatorCost;

        // Compute ideal minimap height from map aspect ratio within content width
        int mmH;
        if (MapWidth > 0 && MapHeight > 0)
        {
            float mapAspect = (float)MapWidth / MapHeight;
            mmH = Math.Max(1, (int)(contentW / mapAspect));
        }
        else
        {
            mmH = contentW; // square fallback
        }

        // Clamp minimap: at least 60px, at most 40% of remaining space
        int minMM = 60;
        int maxMM = Math.Max(minMM, (int)(remainingH * 0.4f));
        mmH = Math.Clamp(mmH, minMM, maxMM);

        int logH = Math.Max(lineH * 3, remainingH - mmH);

        // --- MESSAGE LOG ---
        cy = DrawSectionHeader(sb, font, renderer, "-- LOG --", cx, cy, contentW);
        DrawMessageLog(sb, font, cx, cy, contentW, logH, lineH);

        // --- MINIMAP ---
        int mmY = cy + logH;
        DrawSeparator(renderer, sb, cx, mmY, contentW);
        mmY += separatorCost;
        MinimapRect = new Rectangle(cx, mmY, contentW, mmH);
    }

    private int DrawSectionHeader(SpriteBatch sb, SpriteFont font, Renderer renderer,
        string title, int x, int y, int w)
    {
        int lineH = (int)font.MeasureString("A").Y;
        // Center the header text
        float tw = font.MeasureString(title).X;
        float tx = x + (w - tw) / 2f;
        sb.DrawString(font, title, new Vector2(tx, y), LayoutConstants.SidebarHeaderColor);
        return y + lineH + LayoutConstants.SidebarSectionGap;
    }

    private int DrawStatBar(SpriteBatch sb, SpriteFont font, Renderer renderer,
        string label, string valueText, int x, int y, int contentW,
        int current, int max, Color bgColor, Color fillColor)
    {
        int lineH = (int)font.MeasureString("A").Y;
        int barW = LayoutConstants.SidebarBarWidth;
        int barH = LayoutConstants.SidebarBarHeight;

        // Label
        sb.DrawString(font, label, new Vector2(x, y), LayoutConstants.SidebarTextColor);
        float labelW = font.MeasureString(label + " ").X;

        // Bar background
        int barX = x + (int)labelW;
        int barY = y + (lineH - barH) / 2;
        renderer.DrawRect(sb, new Rectangle(barX, barY, barW, barH), bgColor);

        // Bar fill
        float pct = max > 0 ? Math.Clamp((float)current / max, 0f, 1f) : 0f;
        int fillW = (int)(barW * pct);
        if (fillW > 0)
            renderer.DrawRect(sb, new Rectangle(barX, barY, fillW, barH), fillColor);

        // Value text right of bar
        float valueX = barX + barW + 4;
        sb.DrawString(font, valueText, new Vector2(valueX, y), LayoutConstants.SidebarTextColor);

        return y + lineH + 2;
    }

    private int DrawSeparator(Renderer renderer, SpriteBatch sb, int x, int y, int w)
    {
        renderer.DrawRect(sb, new Rectangle(x, y, w, LayoutConstants.SidebarSeparatorHeight),
            LayoutConstants.SidebarSeparator);
        return y + LayoutConstants.SidebarSeparatorHeight + LayoutConstants.SidebarSectionGap;
    }

    private int DrawEquipment(SpriteBatch sb, SpriteFont font, int x, int y, int contentW, int lineH)
    {
        var equipment = _gsm.State?.Player?.Equipment;
        if (equipment == null || equipment.Count == 0)
        {
            sb.DrawString(font, "(none)", new Vector2(x, y), LayoutConstants.SidebarDimTextColor);
            return y + lineH + 2;
        }

        foreach (var slot in new[] { "Weapon", "Armor", "Accessory" })
        {
            string item = equipment.TryGetValue(slot, out var v) ? v : null;
            string line = $"{slot}: {item ?? "-"}";
            Color color = item != null ? LayoutConstants.SidebarTextColor : LayoutConstants.SidebarDimTextColor;
            y = DrawWrappedText(sb, font, line, x, y, contentW, lineH, color);
        }

        return y + 2;
    }

    private int DrawInventory(SpriteBatch sb, SpriteFont font, int x, int y, int contentW, int lineH)
    {
        var inventory = _gsm.State?.Player?.Inventory;
        if (inventory == null || inventory.Count == 0)
        {
            sb.DrawString(font, "(empty)", new Vector2(x, y), LayoutConstants.SidebarDimTextColor);
            return y + lineH + 2;
        }

        // Group identical items and show count
        var counts = new Dictionary<string, int>();
        foreach (var item in inventory)
        {
            if (!counts.ContainsKey(item)) counts[item] = 0;
            counts[item]++;
        }

        int maxItems = 8; // Show at most 8 item types to leave room for log
        int shown = 0;
        foreach (var (item, count) in counts)
        {
            if (shown >= maxItems)
            {
                sb.DrawString(font, $"... +{counts.Count - maxItems} more",
                    new Vector2(x, y), LayoutConstants.SidebarDimTextColor);
                y += lineH;
                break;
            }
            string line = count > 1 ? $"{item} x{count}" : item;
            y = DrawWrappedText(sb, font, line, x, y, contentW, lineH, LayoutConstants.SidebarTextColor);
            shown++;
        }

        return y + 2;
    }

    private void DrawMessageLog(SpriteBatch sb, SpriteFont font,
        int x, int y, int contentW, int availableHeight, int lineH)
    {
        if (lineH <= 0) return;
        int maxVisualLines = Math.Max(1, availableHeight / lineH);

        if (_log.Count == 0)
        {
            sb.DrawString(font, "...", new Vector2(x, y), LayoutConstants.SidebarDimTextColor);
            return;
        }

        if (_autoScroll)
        {
            // Build visual lines backwards from the last entry to fill the viewport
            var visualLines = new List<(string Text, Color Color)>();
            bool hasOlder = false;

            for (int i = _log.Count - 1; i >= 0; i--)
            {
                var entry = _log.Entries[i];
                var wrapped = TextUtils.WrapText(font, entry.Text, contentW);

                // Check if adding this entry would overflow
                if (visualLines.Count + wrapped.Count > maxVisualLines)
                {
                    hasOlder = true;
                    break;
                }

                // Prepend wrapped lines (in reverse order, then we'll reverse the whole list)
                for (int w = wrapped.Count - 1; w >= 0; w--)
                    visualLines.Add((wrapped[w], entry.Color));
            }

            // Reverse since we built it back-to-front
            visualLines.Reverse();

            // Draw older-content indicator at top if we couldn't fit everything
            if (hasOlder)
            {
                string olderHint = "-- older --";
                float hintW = font.MeasureString(olderHint).X;
                sb.DrawString(font, olderHint, new Vector2(x + (contentW - hintW) / 2f, y),
                    LayoutConstants.SidebarDimTextColor);
                y += lineH;
                // Trim the top lines to make room for the hint
                int drawableLines = maxVisualLines - 1;
                if (visualLines.Count > drawableLines)
                    visualLines.RemoveRange(0, visualLines.Count - drawableLines);
            }

            foreach (var (text, color) in visualLines)
            {
                sb.DrawString(font, text, new Vector2(x, y), color * 0.9f);
                y += lineH;
            }
        }
        else
        {
            // Manual scroll: render forward from _logScrollOffset
            var visualLines = new List<(string Text, Color Color)>();
            bool hasMore = false;

            int startEntry = Math.Clamp(_logScrollOffset, 0, _log.Count);
            for (int i = startEntry; i < _log.Count; i++)
            {
                var entry = _log.Entries[i];
                var wrapped = TextUtils.WrapText(font, entry.Text, contentW);
                foreach (var line in wrapped)
                {
                    if (visualLines.Count >= maxVisualLines)
                    {
                        hasMore = true;
                        break;
                    }
                    visualLines.Add((line, entry.Color));
                }
                if (hasMore) break;
            }

            // Reserve last line for scroll hint
            if (hasMore && visualLines.Count > 1)
            {
                visualLines.RemoveAt(visualLines.Count - 1);
            }

            foreach (var (text, color) in visualLines)
            {
                sb.DrawString(font, text, new Vector2(x, y), color * 0.9f);
                y += lineH;
            }

            if (hasMore)
            {
                string scrollHint = "-- more --";
                float hintW = font.MeasureString(scrollHint).X;
                sb.DrawString(font, scrollHint, new Vector2(x + (contentW - hintW) / 2f, y),
                    LayoutConstants.SidebarDimTextColor);
            }
        }
    }

    /// <summary>
    /// Draws text with word wrapping, returning the new Y cursor position.
    /// </summary>
    private static int DrawWrappedText(SpriteBatch sb, SpriteFont font,
        string text, int x, int y, int contentW, int lineH, Color color)
    {
        var lines = TextUtils.WrapText(font, text, contentW);
        foreach (var line in lines)
        {
            sb.DrawString(font, line, new Vector2(x, y), color);
            y += lineH;
        }
        return y;
    }

    private static string FormatEffect(StatusEffect effect)
    {
        string name = effect.Type?.ToUpperInvariant() switch
        {
            "FIRE" => "Burning",
            "POISON" => "Poisoned",
            "ICE" => "Chilled",
            _ => effect.Type ?? "???"
        };
        return $"[{name} {effect.RemainingSteps}]";
    }

    private static Color EffectColor(StatusEffect effect)
    {
        return effect.Type switch
        {
            "fire" => Color.OrangeRed,
            "poison" => new Color(180, 50, 220),
            "ice" => Color.CornflowerBlue,
            _ => Color.White,
        };
    }
}
