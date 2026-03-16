using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;

namespace TileForge.UI;

/// <summary>
/// Queues dropdown and combobox updates during Draw for processing in the next Update.
/// This avoids collision between nested interactive widgets rendered in the same frame.
/// </summary>
public class DeferredInputQueue
{
    private readonly List<(Dropdown Dropdown, Rectangle Rect)> _dropdowns = new();
    private readonly List<(ComboBox ComboBox, Rectangle Rect)> _comboBoxes = new();

    public void EnqueueDropdown(Dropdown dropdown, Rectangle rect)
    {
        _dropdowns.Add((dropdown, rect));
    }

    public void EnqueueComboBox(ComboBox comboBox, Rectangle rect)
    {
        _comboBoxes.Add((comboBox, rect));
    }

    /// <summary>
    /// Processes all queued widgets using the raw MouseState overloads.
    /// Call once per Update, after collecting all widgets during Draw.
    /// </summary>
    public void ProcessAll(MouseState mouse, MouseState prevMouse, SpriteFont font,
                           int screenW, int screenH)
    {
        foreach (var (dd, rect) in _dropdowns)
            dd.Update(mouse, prevMouse, rect, font, screenW, screenH);

        foreach (var (cb, rect) in _comboBoxes)
            cb.UpdateMouse(mouse, prevMouse, rect, font, screenW, screenH);
    }

    public void Clear()
    {
        _dropdowns.Clear();
        _comboBoxes.Clear();
    }
}
