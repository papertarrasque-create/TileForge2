using System;
using System.Collections.Generic;

namespace TileForge.UI;

/// <summary>
/// Maps (menuIndex, itemIndex) pairs from the MenuBar to editor action callbacks.
/// Commands are registered as a dictionary for extensibility and clean wiring.
/// </summary>
public class MenuActionDispatcher
{
    private readonly Dictionary<(int menu, int item), Action> _commands;

    public MenuActionDispatcher(Dictionary<(int menu, int item), Action> commands)
    {
        _commands = commands ?? new();
    }

    /// <summary>
    /// Dispatches a menu click to the appropriate action callback.
    /// If the (menuIndex, itemIndex) pair doesn't map to anything, does nothing.
    /// </summary>
    public void Dispatch(int menuIndex, int itemIndex)
    {
        if (_commands.TryGetValue((menuIndex, itemIndex), out var action))
            action?.Invoke();
    }
}
