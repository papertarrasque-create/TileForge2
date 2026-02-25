using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DojoUI;

public class Selection
{
    private (int col, int row)? _anchor;
    private (int col, int row)? _current;
    private readonly List<(int col, int row)> _cells = new();
    private readonly HashSet<(int col, int row)> _cellSet = new();

    public (int col, int row)? Current => _current;
    public int Count => _cells.Count;

    public void Select(int col, int row, bool shiftHeld, bool ctrlHeld = false)
    {
        _current = (col, row);

        if (ctrlHeld)
        {
            // Toggle individual cell
            if (_cellSet.Add((col, row)))
                _cells.Add((col, row));
            else
            {
                _cellSet.Remove((col, row));
                _cells.Remove((col, row));
            }
            _anchor = (col, row);
        }
        else if (shiftHeld && _anchor.HasValue)
        {
            // Add rectangular range from anchor to click
            int minCol = Math.Min(_anchor.Value.col, col);
            int minRow = Math.Min(_anchor.Value.row, row);
            int maxCol = Math.Max(_anchor.Value.col, col);
            int maxRow = Math.Max(_anchor.Value.row, row);
            for (int r = minRow; r <= maxRow; r++)
                for (int c = minCol; c <= maxCol; c++)
                    if (_cellSet.Add((c, r)))
                        _cells.Add((c, r));
        }
        else
        {
            // Clear and start fresh
            _cells.Clear();
            _cellSet.Clear();
            _cells.Add((col, row));
            _cellSet.Add((col, row));
            _anchor = (col, row);
        }
    }

    public void AddCell(int col, int row)
    {
        if (_cellSet.Add((col, row)))
            _cells.Add((col, row));
        _anchor ??= (col, row);
        _current ??= (col, row);
    }

    public void Clear()
    {
        _anchor = null;
        _current = null;
        _cells.Clear();
        _cellSet.Clear();
    }

    public IReadOnlyCollection<(int col, int row)> GetSelectedCells() => _cells;

    /// <summary>
    /// Returns bounding box of all selected cells. Backward compatible with rectangular selection.
    /// </summary>
    public Rectangle? GetRange()
    {
        if (_cells.Count == 0)
            return null;

        int minCol = int.MaxValue, minRow = int.MaxValue;
        int maxCol = int.MinValue, maxRow = int.MinValue;
        foreach (var (col, row) in _cells)
        {
            if (col < minCol) minCol = col;
            if (row < minRow) minRow = row;
            if (col > maxCol) maxCol = col;
            if (row > maxRow) maxRow = row;
        }

        return new Rectangle(minCol, minRow, maxCol - minCol + 1, maxRow - minRow + 1);
    }

    public bool Contains(int col, int row) => _cellSet.Contains((col, row));
}
