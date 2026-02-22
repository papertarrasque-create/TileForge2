using System.Collections.Generic;
using TileForge2.Data;
using TileForge2.Editor.Tools;
using TileForge2.Play;
using DojoUI;

namespace TileForge2.Editor;

public class EditorState
{
    public MapData Map { get; set; }
    public List<TileGroup> Groups { get; set; } = new();
    public Dictionary<string, TileGroup> GroupsByName { get; private set; } = new();

    public SpriteSheet Sheet { get; set; }
    public string SheetPath { get; set; }

    public UndoStack UndoStack { get; } = new();

    public ITool ActiveTool { get; set; }
    public string ActiveLayerName { get; set; } = "Ground";
    public string SelectedGroupName { get; set; }
    public string SelectedEntityId { get; set; }

    // Play mode
    public bool IsPlayMode { get; set; }
    public PlayState PlayState { get; set; }

    public MapLayer ActiveLayer
    {
        get => Map?.GetLayer(ActiveLayerName);
    }

    public TileGroup SelectedGroup
    {
        get
        {
            if (SelectedGroupName != null && GroupsByName.TryGetValue(SelectedGroupName, out var g))
                return g;
            return null;
        }
    }

    public void RebuildGroupIndex()
    {
        GroupsByName = new Dictionary<string, TileGroup>();
        foreach (var group in Groups)
            GroupsByName[group.Name] = group;
    }

    public void AddGroup(TileGroup group)
    {
        Groups.Add(group);
        GroupsByName[group.Name] = group;
    }

    public void RemoveGroup(string name)
    {
        if (!GroupsByName.TryGetValue(name, out var group)) return;

        Groups.Remove(group);
        GroupsByName.Remove(name);

        // Clear all map cells referencing this group
        if (Map != null)
        {
            foreach (var layer in Map.Layers)
            {
                for (int i = 0; i < layer.Cells.Length; i++)
                {
                    if (layer.Cells[i] == name)
                        layer.Cells[i] = null;
                }
            }

            Map.Entities.RemoveAll(e => e.GroupName == name);
        }

        if (SelectedGroupName == name)
            SelectedGroupName = Groups.Count > 0 ? Groups[0].Name : null;
    }

    public void RenameGroup(string oldName, string newName)
    {
        if (!GroupsByName.TryGetValue(oldName, out var group)) return;
        if (GroupsByName.ContainsKey(newName)) return;

        group.Name = newName;
        GroupsByName.Remove(oldName);
        GroupsByName[newName] = group;

        if (Map != null)
        {
            foreach (var layer in Map.Layers)
            {
                for (int i = 0; i < layer.Cells.Length; i++)
                {
                    if (layer.Cells[i] == oldName)
                        layer.Cells[i] = newName;
                }
            }

            foreach (var entity in Map.Entities)
            {
                if (entity.GroupName == oldName)
                    entity.GroupName = newName;
            }
        }

        if (SelectedGroupName == oldName)
            SelectedGroupName = newName;
    }
}
