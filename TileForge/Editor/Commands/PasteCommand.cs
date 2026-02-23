using TileForge.Data;

namespace TileForge.Editor.Commands;

public class PasteCommand : ICommand
{
    private readonly MapData _map;
    private readonly string _layerName;
    private readonly int _targetX;
    private readonly int _targetY;
    private readonly TileClipboard _clipboard;
    private readonly string[] _originalCells; // saved for undo

    public PasteCommand(MapData map, string layerName, int targetX, int targetY, TileClipboard clipboard)
    {
        _map = map;
        _layerName = layerName;
        _targetX = targetX;
        _targetY = targetY;
        _clipboard = clipboard;

        // Snapshot the cells that will be overwritten
        _originalCells = new string[clipboard.Width * clipboard.Height];
        var layer = map.GetLayer(layerName);
        if (layer != null)
        {
            for (int cy = 0; cy < clipboard.Height; cy++)
            {
                for (int cx = 0; cx < clipboard.Width; cx++)
                {
                    int mapX = targetX + cx;
                    int mapY = targetY + cy;
                    if (map.InBounds(mapX, mapY))
                        _originalCells[cx + cy * clipboard.Width] = layer.GetCell(mapX, mapY, map.Width);
                }
            }
        }
    }

    public void Execute()
    {
        var layer = _map.GetLayer(_layerName);
        if (layer == null) return;

        for (int cy = 0; cy < _clipboard.Height; cy++)
        {
            for (int cx = 0; cx < _clipboard.Width; cx++)
            {
                int mapX = _targetX + cx;
                int mapY = _targetY + cy;
                if (!_map.InBounds(mapX, mapY)) continue;

                string cell = _clipboard.GetCell(cx, cy);
                if (cell != null)
                    layer.SetCell(mapX, mapY, _map.Width, cell);
            }
        }
    }

    public void Undo()
    {
        var layer = _map.GetLayer(_layerName);
        if (layer == null) return;

        for (int cy = 0; cy < _clipboard.Height; cy++)
        {
            for (int cx = 0; cx < _clipboard.Width; cx++)
            {
                int mapX = _targetX + cx;
                int mapY = _targetY + cy;
                if (!_map.InBounds(mapX, mapY)) continue;

                layer.SetCell(mapX, mapY, _map.Width, _originalCells[cx + cy * _clipboard.Width]);
            }
        }
    }
}
