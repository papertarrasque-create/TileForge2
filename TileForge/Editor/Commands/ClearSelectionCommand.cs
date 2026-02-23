using Microsoft.Xna.Framework;
using TileForge.Data;

namespace TileForge.Editor.Commands;

public class ClearSelectionCommand : ICommand
{
    private readonly MapData _map;
    private readonly string _layerName;
    private readonly Rectangle _selection; // grid coords
    private readonly string[] _originalCells; // saved for undo

    public ClearSelectionCommand(MapData map, string layerName, Rectangle selection)
    {
        _map = map;
        _layerName = layerName;
        _selection = selection;

        // Snapshot the cells that will be cleared
        _originalCells = new string[selection.Width * selection.Height];
        var layer = map.GetLayer(layerName);
        if (layer != null)
        {
            for (int cy = 0; cy < selection.Height; cy++)
            {
                for (int cx = 0; cx < selection.Width; cx++)
                {
                    int mapX = selection.X + cx;
                    int mapY = selection.Y + cy;
                    if (map.InBounds(mapX, mapY))
                        _originalCells[cx + cy * selection.Width] = layer.GetCell(mapX, mapY, map.Width);
                }
            }
        }
    }

    public void Execute()
    {
        var layer = _map.GetLayer(_layerName);
        if (layer == null) return;

        for (int cy = 0; cy < _selection.Height; cy++)
        {
            for (int cx = 0; cx < _selection.Width; cx++)
            {
                int mapX = _selection.X + cx;
                int mapY = _selection.Y + cy;
                if (_map.InBounds(mapX, mapY))
                    layer.SetCell(mapX, mapY, _map.Width, null);
            }
        }
    }

    public void Undo()
    {
        var layer = _map.GetLayer(_layerName);
        if (layer == null) return;

        for (int cy = 0; cy < _selection.Height; cy++)
        {
            for (int cx = 0; cx < _selection.Width; cx++)
            {
                int mapX = _selection.X + cx;
                int mapY = _selection.Y + cy;
                if (_map.InBounds(mapX, mapY))
                    layer.SetCell(mapX, mapY, _map.Width, _originalCells[cx + cy * _selection.Width]);
            }
        }
    }
}
