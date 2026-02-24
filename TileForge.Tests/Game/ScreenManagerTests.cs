using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class ScreenManagerTests
{
    private class MockScreen : GameScreen
    {
        public bool EnterCalled { get; private set; }
        public bool ExitCalled { get; private set; }
        public bool UpdateCalled { get; private set; }
        public bool DrawCalled { get; private set; }
        public int UpdateOrder { get; private set; }
        public int DrawOrder { get; private set; }
        private static int _orderCounter;
        public static void ResetOrder() => _orderCounter = 0;

        public override void OnEnter() => EnterCalled = true;
        public override void OnExit() => ExitCalled = true;
        public override void Update(GameTime gameTime, GameInputManager input)
        {
            UpdateCalled = true;
            UpdateOrder = ++_orderCounter;
        }
        public override void Draw(SpriteBatch sb, SpriteFont font, Renderer r, Rectangle bounds)
        {
            DrawCalled = true;
            DrawOrder = ++_orderCounter;
        }
    }

    private class MockOverlay : MockScreen
    {
        public override bool IsOverlay => true;
    }

    private static GameTime MakeGameTime() =>
        new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

    [Fact]
    public void Push_SetsScreenManagerOnScreen()
    {
        var manager = new ScreenManager();
        var screen = new MockScreen();

        manager.Push(screen);

        Assert.Same(manager, screen.ScreenManager);
    }

    [Fact]
    public void Push_CallsOnEnter()
    {
        var manager = new ScreenManager();
        var screen = new MockScreen();

        manager.Push(screen);

        Assert.True(screen.EnterCalled);
    }

    [Fact]
    public void Pop_CallsOnExitOnTopScreen()
    {
        var manager = new ScreenManager();
        var screen = new MockScreen();
        manager.Push(screen);

        manager.Pop();

        Assert.True(screen.ExitCalled);
    }

    [Fact]
    public void Pop_OnEmpty_DoesNotThrow()
    {
        var manager = new ScreenManager();

        var exception = Record.Exception(() => manager.Pop());

        Assert.Null(exception);
    }

    [Fact]
    public void Clear_CallsOnExitOnAllScreens()
    {
        var manager = new ScreenManager();
        var screen1 = new MockScreen();
        var screen2 = new MockScreen();
        manager.Push(screen1);
        manager.Push(screen2);

        manager.Clear();

        Assert.True(screen1.ExitCalled);
        Assert.True(screen2.ExitCalled);
    }

    [Fact]
    public void Clear_EmptiesTheStack()
    {
        var manager = new ScreenManager();
        manager.Push(new MockScreen());
        manager.Push(new MockScreen());

        manager.Clear();

        Assert.False(manager.HasScreens);
    }

    [Fact]
    public void Update_OnlyTopScreenReceivesUpdate()
    {
        var manager = new ScreenManager();
        var bottom = new MockScreen();
        var top = new MockScreen();
        manager.Push(bottom);
        manager.Push(top);

        manager.Update(MakeGameTime(), new GameInputManager());

        Assert.False(bottom.UpdateCalled);
        Assert.True(top.UpdateCalled);
    }

    [Fact]
    public void Draw_OverlayOnTop_BothScreensDrawn_NonOverlayFirst()
    {
        MockScreen.ResetOrder();
        var manager = new ScreenManager();
        var baseScreen = new MockScreen();
        var overlay = new MockOverlay();
        manager.Push(baseScreen);
        manager.Push(overlay);

        manager.Draw(null, null, null, Rectangle.Empty);

        Assert.True(baseScreen.DrawCalled);
        Assert.True(overlay.DrawCalled);
        Assert.True(baseScreen.DrawOrder < overlay.DrawOrder);
    }

    [Fact]
    public void Draw_NonOverlayOnTop_OnlyTopScreenDrawn()
    {
        var manager = new ScreenManager();
        var bottom = new MockScreen();
        var top = new MockScreen();
        manager.Push(bottom);
        manager.Push(top);

        manager.Draw(null, null, null, Rectangle.Empty);

        Assert.False(bottom.DrawCalled);
        Assert.True(top.DrawCalled);
    }

    [Fact]
    public void HasScreens_ReflectsState()
    {
        var manager = new ScreenManager();

        Assert.False(manager.HasScreens);

        manager.Push(new MockScreen());
        Assert.True(manager.HasScreens);

        manager.Pop();
        Assert.False(manager.HasScreens);
    }

    [Fact]
    public void ExitRequested_DefaultsToFalse()
    {
        var manager = new ScreenManager();

        Assert.False(manager.ExitRequested);
    }
}
