namespace TileForge2.Data;

public class MapLayer
{
    public string Name { get; set; }
    public bool Visible { get; set; } = true;
    public string[] Cells { get; set; }

    public MapLayer(string name, int width, int height)
    {
        Name = name;
        Cells = new string[width * height];
    }

    public string GetCell(int x, int y, int width)
    {
        int index = x + y * width;
        if (index < 0 || index >= Cells.Length) return null;
        return Cells[index];
    }

    public void SetCell(int x, int y, int width, string groupName)
    {
        int index = x + y * width;
        if (index >= 0 && index < Cells.Length)
            Cells[index] = groupName;
    }
}
