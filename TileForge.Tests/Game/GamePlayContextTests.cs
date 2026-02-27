using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Xunit;
using TileForge.Game;
using TileForge.Tests.Helpers;

namespace TileForge.Tests.Game;

public class GamePlayContextTests
{
    [Fact]
    public void Constructor_StoresAllDependencies()
    {
        var gsm = new GameStateManager();
        var save = new SaveManager("/test/saves");
        var input = new GameInputManager();
        var quest = new QuestManager(new List<QuestDefinition>());
        Func<Rectangle> bounds = () => new Rectangle(0, 0, 800, 600);
        var loader = new MockDialogueLoader();

        var context = new GamePlayContext(gsm, save, input, "/test/bindings.json",
            quest, bounds, dialogueLoader: loader);

        Assert.Same(gsm, context.StateManager);
        Assert.Same(save, context.SaveManager);
        Assert.Same(input, context.InputManager);
        Assert.Equal("/test/bindings.json", context.BindingsPath);
        Assert.Same(quest, context.QuestManager);
        Assert.Same(bounds, context.GetCanvasBounds);
        Assert.Same(loader, context.DialogueLoader);
        Assert.Null(context.EdgeResolver);
    }

    [Fact]
    public void Constructor_NullEdgeResolver_IsAccepted()
    {
        var context = new GamePlayContext(
            new GameStateManager(), new SaveManager("/test/saves"),
            new GameInputManager(), "/bindings",
            new QuestManager(new List<QuestDefinition>()),
            () => Rectangle.Empty);

        Assert.Null(context.EdgeResolver);
        Assert.Null(context.DialogueLoader);
    }
}
