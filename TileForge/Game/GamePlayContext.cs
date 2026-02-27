using System;
using Microsoft.Xna.Framework;
using TileForge.Infrastructure;

namespace TileForge.Game;

/// <summary>
/// Groups game runtime dependencies for GameplayScreen.
/// Reduces constructor parameter count and eliminates duplication
/// when constructing GameplayScreen in multiple places.
/// </summary>
public class GamePlayContext
{
    public GameStateManager StateManager { get; }
    public SaveManager SaveManager { get; }
    public GameInputManager InputManager { get; }
    public string BindingsPath { get; }
    public QuestManager QuestManager { get; }
    public Func<Rectangle> GetCanvasBounds { get; }
    public EdgeTransitionResolver EdgeResolver { get; }
    public IDialogueLoader DialogueLoader { get; }

    public GamePlayContext(
        GameStateManager stateManager,
        SaveManager saveManager,
        GameInputManager inputManager,
        string bindingsPath,
        QuestManager questManager,
        Func<Rectangle> getCanvasBounds,
        EdgeTransitionResolver edgeResolver = null,
        IDialogueLoader dialogueLoader = null)
    {
        StateManager = stateManager;
        SaveManager = saveManager;
        InputManager = inputManager;
        BindingsPath = bindingsPath;
        QuestManager = questManager;
        GetCanvasBounds = getCanvasBounds;
        EdgeResolver = edgeResolver;
        DialogueLoader = dialogueLoader;
    }
}
