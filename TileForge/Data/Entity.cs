using System;
using System.Collections.Generic;

namespace TileForge.Data;

public class Entity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string GroupName { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}
