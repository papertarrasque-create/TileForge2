namespace TileForge.Editor;

public class TileClipboard
{
    public int Width { get; }
    public int Height { get; }
    public string[] Cells { get; }  // width * height, row-major

    public TileClipboard(int width, int height, string[] cells)
    {
        Width = width;
        Height = height;
        Cells = cells;
    }

    public string GetCell(int x, int y) => Cells[x + y * Width];
}
