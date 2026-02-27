using System.Collections.Generic;

namespace TileForge.Data;

/// <summary>
/// Project-level spatial arrangement of maps on a 2D grid.
/// Adjacency is determined by grid position: maps in neighboring cells are neighbors.
/// Auto-bidirectional — placing Map A at (0,0) and Map B at (1,0) makes them mutual E/W neighbors.
/// </summary>
public class WorldLayout
{
    /// <summary>Key = map name (matches MapDocumentState.Name). Value = grid placement.</summary>
    public Dictionary<string, MapPlacement> Maps { get; set; } = new();
}

/// <summary>
/// A map's position on the world grid, plus optional spawn-point overrides per entry direction.
/// </summary>
public class MapPlacement
{
    public int GridX { get; set; }
    public int GridY { get; set; }

    /// <summary>Where the player spawns when entering this map from the north (walked south from the map above). Null = default.</summary>
    public EdgeSpawn NorthEntry { get; set; }

    /// <summary>Where the player spawns when entering this map from the south (walked north from the map below). Null = default.</summary>
    public EdgeSpawn SouthEntry { get; set; }

    /// <summary>Where the player spawns when entering this map from the east (walked west from the map to the right). Null = default.</summary>
    public EdgeSpawn EastEntry { get; set; }

    /// <summary>Where the player spawns when entering this map from the west (walked east from the map to the left). Null = default.</summary>
    public EdgeSpawn WestEntry { get; set; }

    /// <summary>Tile on this map that portals to the north neighbor when stepped on. Null = edge-of-map only.</summary>
    public EdgeSpawn NorthExit { get; set; }

    /// <summary>Tile on this map that portals to the south neighbor when stepped on. Null = edge-of-map only.</summary>
    public EdgeSpawn SouthExit { get; set; }

    /// <summary>Tile on this map that portals to the east neighbor when stepped on. Null = edge-of-map only.</summary>
    public EdgeSpawn EastExit { get; set; }

    /// <summary>Tile on this map that portals to the west neighbor when stepped on. Null = edge-of-map only.</summary>
    public EdgeSpawn WestExit { get; set; }
}

/// <summary>
/// A specific tile coordinate for spawning the player when entering a map from a given direction.
/// When null on a MapPlacement entry, the default spawn logic is used:
/// opposite edge, parallel coordinate clamped to target map bounds.
/// </summary>
public class EdgeSpawn
{
    public int X { get; set; }
    public int Y { get; set; }
}
