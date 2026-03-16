using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class GameWorldViewTests
{
    [Fact]
    public void GameWorldView_CanBeConstructed_WithEmptyCollections()
    {
        var view = new GameWorldView
        {
            Maps = new(),
            ActiveMapId = "town",
            Groups = new(),
            Dialogues = new(),
            Quests = new(),
        };

        Assert.Equal("town", view.ActiveMapId);
        Assert.Empty(view.Maps);
    }

    [Fact]
    public void MapView_ContainsLayersAndDimensions()
    {
        var map = new MapView
        {
            Width = 20,
            Height = 15,
            Layers = new()
            {
                new LayerView
                {
                    Tiles = new int[15, 20],
                    Entities = new()
                    {
                        new EntityView
                        {
                            Id = "npc_1",
                            GroupName = "elder",
                            X = 5,
                            Y = 3,
                            Properties = new() { ["dialogue_id"] = "elder_01" }
                        }
                    }
                }
            }
        };

        Assert.Equal(20, map.Width);
        Assert.Single(map.Layers);
        Assert.Single(map.Layers[0].Entities);
        Assert.Equal("npc_1", map.Layers[0].Entities[0].Id);
    }

    [Fact]
    public void GroupView_StoresNameTypeAndProperties()
    {
        var group = new GroupView
        {
            Name = "npc_elder",
            Type = TileForge.Data.GroupType.Entity,
            SpriteIndex = 42,
            Properties = new() { ["behavior"] = "idle" }
        };

        Assert.Equal("npc_elder", group.Name);
        Assert.Equal(TileForge.Data.GroupType.Entity, group.Type);
        Assert.Equal(42, group.SpriteIndex);
        Assert.Equal("idle", group.Properties["behavior"]);
    }

    [Fact]
    public void EntityView_FullRoundTrip()
    {
        var entity = new EntityView
        {
            Id = "trigger_1",
            GroupName = "cave_entrance",
            X = 10,
            Y = 7,
            Properties = new()
            {
                ["dialogue_id"] = "cave_warning",
                ["target_map"] = "cave_level1",
            }
        };

        Assert.Equal("trigger_1", entity.Id);
        Assert.Equal(10, entity.X);
        Assert.Equal(2, entity.Properties.Count);
    }
}
