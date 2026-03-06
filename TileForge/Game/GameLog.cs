using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileForge.Game;

/// <summary>
/// Persistent message log for the game session. Captures combat, dialogue,
/// quest, and interaction messages in a scrollable history buffer.
/// Inspired by classic CRPG message logs (Caves of Qud, Ultima, etc.).
/// </summary>
public class GameLog
{
    public const int MaxEntries = 200;

    private readonly List<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => _entries;
    public int Count => _entries.Count;

    /// <summary>
    /// Incremented each time a new entry is added. Consumers can compare
    /// against their last-seen version to detect new messages.
    /// </summary>
    public int Version { get; private set; }

    public void Add(string text, Color color)
    {
        _entries.Add(new LogEntry(text, color));
        if (_entries.Count > MaxEntries)
            _entries.RemoveAt(0);
        Version++;
    }

    public void Clear()
    {
        _entries.Clear();
        Version++;
    }
}

public class LogEntry
{
    public string Text { get; }
    public Color Color { get; }

    public LogEntry(string text, Color color)
    {
        Text = text;
        Color = color;
    }
}
