using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TileForge.Data;

namespace TileForge.Editor;

/// <summary>
/// Per-map editor state: map data, undo history, camera, active layer, selection.
/// One instance per map tab in a multimap project.
/// </summary>
public class MapDocumentState
{
    public string Name { get; set; }
    public MapData Map { get; set; }
    public UndoStack UndoStack { get; } = new();

    // Per-map camera state (saved/restored on tab switch)
    public float CameraX { get; set; }
    public float CameraY { get; set; }
    public int ZoomIndex { get; set; } = 1;

    // Per-map editing state
    public string ActiveLayerName { get; set; } = "Ground";
    public string SelectedEntityId { get; set; }
    public Rectangle? TileSelection { get; set; }

    // Per-map MapPanel collapse state
    public HashSet<string> CollapsedLayers { get; set; } = new();
}
