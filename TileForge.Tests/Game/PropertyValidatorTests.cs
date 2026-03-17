using System.Collections.Generic;
using System.Linq;
using Xunit;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class PropertyValidatorTests
{
    private static EntityInstance MakeEntity(string id, params (string key, string value)[] props)
    {
        var entity = new EntityInstance { Id = id, DefinitionName = "Test", Properties = new() };
        foreach (var (k, v) in props)
            entity.Properties[k] = v;
        return entity;
    }

    [Fact]
    public void Validate_ValidProperties_NoErrors()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("e1", ("health", "100"), ("behavior", "idle")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_UnknownProperty_Warning()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("e1", ("custom_prop", "value")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Single(errors);
        Assert.Equal(PropertyErrorLevel.Warning, errors[0].Level);
        Assert.Equal("custom_prop", errors[0].Key);
        Assert.Contains("Unknown", errors[0].Message);
    }

    [Fact]
    public void Validate_InvalidInt_Error()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("e1", ("health", "abc")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Single(errors);
        Assert.Equal(PropertyErrorLevel.Error, errors[0].Level);
        Assert.Contains("Expected integer", errors[0].Message);
    }

    [Fact]
    public void Validate_IntOutOfRange_Error()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("e1", ("health", "0")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Single(errors);
        Assert.Equal(PropertyErrorLevel.Error, errors[0].Level);
        Assert.Contains("outside range", errors[0].Message);
    }

    [Fact]
    public void Validate_InvalidEnum_Error()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("e1", ("behavior", "flee")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Single(errors);
        Assert.Equal(PropertyErrorLevel.Error, errors[0].Level);
        Assert.Contains("not in", errors[0].Message);
    }

    [Fact]
    public void Validate_InvalidBool_Error()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("e1", ("hostile", "yes")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Single(errors);
        Assert.Equal(PropertyErrorLevel.Error, errors[0].Level);
        Assert.Contains("Expected true/false", errors[0].Message);
    }

    [Fact]
    public void Validate_EmptyList_NoErrors()
    {
        var errors = PropertyValidator.Validate(new List<EntityInstance>());
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MultipleEntities_CollectsAll()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("e1", ("health", "abc")),
            MakeEntity("e2", ("behavior", "flee")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Equal(2, errors.Count);
        Assert.Equal("e1", errors[0].EntityId);
        Assert.Equal("e2", errors[1].EntityId);
    }

    [Fact]
    public void Validate_EntityId_IncludedInError()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("goblin_01", ("health", "abc")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Equal("goblin_01", errors[0].EntityId);
    }
}
