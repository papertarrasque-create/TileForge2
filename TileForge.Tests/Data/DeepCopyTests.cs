using System.Collections.Generic;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Data;

public class DeepCopyTests
{
    [Fact]
    public void Entity_DeepCopy_CreatesIndependentCopy()
    {
        var original = new Entity
        {
            Id = "abc",
            GroupName = "goblin",
            X = 5,
            Y = 10,
            Properties = new Dictionary<string, string> { { "hp", "10" } },
        };

        var copy = original.DeepCopy();

        Assert.Equal("abc", copy.Id);
        Assert.Equal("goblin", copy.GroupName);
        Assert.Equal(5, copy.X);
        Assert.Equal(10, copy.Y);
        Assert.Equal("10", copy.Properties["hp"]);

        // Mutating copy does not affect original
        copy.X = 99;
        copy.Properties["hp"] = "0";
        Assert.Equal(5, original.X);
        Assert.Equal("10", original.Properties["hp"]);
    }

    [Fact]
    public void MapLayer_DeepCopy_CreatesIndependentCopy()
    {
        var original = new MapLayer("Ground", 4, 4);
        original.SetCell(1, 2, 4, "grass");

        var copy = original.DeepCopy(4, 4);

        Assert.Equal("Ground", copy.Name);
        Assert.Equal("grass", copy.GetCell(1, 2, 4));

        // Mutating copy does not affect original
        copy.SetCell(1, 2, 4, "stone");
        Assert.Equal("grass", original.GetCell(1, 2, 4));
    }

    [Fact]
    public void MapData_DeepCopy_CreatesIndependentCopy()
    {
        var original = new MapData(10, 10);
        original.Layers[0].SetCell(0, 0, 10, "grass");
        original.Entities.Add(new Entity { Id = "e1", GroupName = "goblin", X = 3, Y = 4 });
        original.EntityRenderOrder = 1;

        var copy = original.DeepCopy();

        Assert.Equal(10, copy.Width);
        Assert.Equal(10, copy.Height);
        Assert.Equal(2, copy.Layers.Count);
        Assert.Equal("grass", copy.Layers[0].GetCell(0, 0, 10));
        Assert.Single(copy.Entities);
        Assert.Equal("e1", copy.Entities[0].Id);
        Assert.Equal(1, copy.EntityRenderOrder);

        // Mutating copy does not affect original
        copy.Entities[0].X = 99;
        copy.Entities.RemoveAt(0);
        copy.Layers[0].SetCell(0, 0, 10, "dirt");
        Assert.Equal(3, original.Entities[0].X);
        Assert.Single(original.Entities);
        Assert.Equal("grass", original.Layers[0].GetCell(0, 0, 10));
    }

    [Fact]
    public void TileGroup_DeepCopy_CreatesIndependentCopy()
    {
        var original = new TileGroup
        {
            Name = "goblin",
            Type = GroupType.Entity,
            Sprites = new List<SpriteRef> { new() { Col = 1, Row = 2 } },
            IsSolid = true,
            IsPlayer = false,
            IsPassable = false,
            MovementCost = 2.0f,
            EntityType = EntityType.NPC,
            DefaultProperties = new Dictionary<string, string> { { "hp", "10" } },
        };

        var copy = original.DeepCopy();

        Assert.Equal("goblin", copy.Name);
        Assert.Equal(GroupType.Entity, copy.Type);
        Assert.Single(copy.Sprites);
        Assert.Equal(1, copy.Sprites[0].Col);
        Assert.True(copy.IsSolid);
        Assert.False(copy.IsPassable);
        Assert.Equal(2.0f, copy.MovementCost);
        Assert.Equal(EntityType.NPC, copy.EntityType);
        Assert.Equal("10", copy.DefaultProperties["hp"]);

        // Mutating copy does not affect original
        copy.DefaultProperties["hp"] = "0";
        copy.Sprites[0].Col = 99;
        Assert.Equal("10", original.DefaultProperties["hp"]);
        Assert.Equal(1, original.Sprites[0].Col);
    }

    [Fact]
    public void MapDocumentState_DeepCopy_CreatesIndependentCopy()
    {
        var original = new MapDocumentState
        {
            Name = "TestMap",
            Map = new MapData(5, 5),
            CameraX = 100f,
            CameraY = 200f,
            ZoomIndex = 3,
            ActiveLayerName = "Objects",
        };
        original.Map.Entities.Add(new Entity { Id = "e1", GroupName = "npc", X = 1, Y = 1 });
        original.CollapsedLayers.Add("Ground");

        var copy = original.DeepCopy();

        Assert.Equal("TestMap", copy.Name);
        Assert.Equal(5, copy.Map.Width);
        Assert.Single(copy.Map.Entities);
        Assert.Equal("e1", copy.Map.Entities[0].Id);
        Assert.Equal(100f, copy.CameraX);
        Assert.Equal(3, copy.ZoomIndex);
        Assert.Equal("Objects", copy.ActiveLayerName);
        Assert.Contains("Ground", copy.CollapsedLayers);

        // Mutating copy does not affect original
        copy.Map.Entities.RemoveAt(0);
        copy.CollapsedLayers.Clear();
        Assert.Single(original.Map.Entities);
        Assert.Contains("Ground", original.CollapsedLayers);
    }
}
