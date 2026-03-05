using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class QuestLogScreenTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static GameStateManager CreateGameStateManager()
    {
        var gsm = new GameStateManager();
        gsm.State.Player = new PlayerState { Health = 100, MaxHealth = 100 };
        return gsm;
    }

    private static QuestManager CreateQuestManager(List<QuestDefinition> quests = null)
    {
        return new QuestManager(quests ?? new List<QuestDefinition>());
    }

    private static QuestDefinition MakeQuest(string id, string startFlag, string completionFlag,
        List<QuestObjective> objectives = null)
    {
        return new QuestDefinition
        {
            Id = id,
            Name = $"Quest: {id}",
            Description = $"Description for {id}",
            StartFlag = startFlag,
            CompletionFlag = completionFlag,
            Objectives = objectives ?? new List<QuestObjective>(),
        };
    }

    private static (QuestLogScreen screen, ScreenManager manager) CreateScreen(
        QuestManager qm = null, GameStateManager gsm = null)
    {
        gsm ??= CreateGameStateManager();
        qm ??= CreateQuestManager();
        var screenManager = new ScreenManager();
        var screen = new QuestLogScreen(qm, gsm);
        screenManager.Push(screen);
        return (screen, screenManager);
    }

    private static GameInputManager SimulateKeyPress(Keys key)
    {
        var input = new GameInputManager();
        input.Update(new KeyboardState());
        input.Update(new KeyboardState(key));
        return input;
    }

    private static readonly GameTime DefaultGameTime =
        new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

    // =========================================================================
    // Construction & overlay
    // =========================================================================

    [Fact]
    public void IsOverlay_ReturnsTrue()
    {
        var gsm = CreateGameStateManager();
        var qm = CreateQuestManager();
        var screen = new QuestLogScreen(qm, gsm);
        Assert.True(screen.IsOverlay);
    }

    // =========================================================================
    // Cancel closes
    // =========================================================================

    [Fact]
    public void Cancel_PopsScreen()
    {
        var (screen, manager) = CreateScreen();

        var cancel = SimulateKeyPress(Keys.X);
        screen.Update(DefaultGameTime, cancel);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // OpenQuestLog toggle closes
    // =========================================================================

    [Fact]
    public void OpenQuestLog_PopsScreen()
    {
        var (screen, manager) = CreateScreen();

        var toggle = SimulateKeyPress(Keys.Q);
        screen.Update(DefaultGameTime, toggle);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // Quest status filtering
    // =========================================================================

    [Fact]
    public void QuestManager_ActiveQuest_VisibleInScreen()
    {
        var gsm = CreateGameStateManager();
        var quest = MakeQuest("q1", "quest_started:q1", "quest_complete:q1",
            new List<QuestObjective>
            {
                new() { Description = "Find key", Type = "flag", Flag = "has_key" },
            });
        var qm = CreateQuestManager(new List<QuestDefinition> { quest });

        gsm.SetFlag("quest_started:q1");

        Assert.Equal(QuestStatus.Active, qm.GetQuestStatus(quest, gsm));
    }

    [Fact]
    public void QuestManager_CompletedQuest_StatusIsCompleted()
    {
        var gsm = CreateGameStateManager();
        var quest = MakeQuest("q1", "quest_started:q1", "quest_complete:q1");
        var qm = CreateQuestManager(new List<QuestDefinition> { quest });

        gsm.SetFlag("quest_started:q1");
        gsm.SetFlag("quest_complete:q1");

        Assert.Equal(QuestStatus.Completed, qm.GetQuestStatus(quest, gsm));
    }

    [Fact]
    public void QuestManager_NotStartedQuest_StatusIsNotStarted()
    {
        var gsm = CreateGameStateManager();
        var quest = MakeQuest("q1", "quest_started:q1", "quest_complete:q1");
        var qm = CreateQuestManager(new List<QuestDefinition> { quest });

        Assert.Equal(QuestStatus.NotStarted, qm.GetQuestStatus(quest, gsm));
    }
}
