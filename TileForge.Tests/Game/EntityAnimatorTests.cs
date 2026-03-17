using Microsoft.Xna.Framework;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class EntityAnimatorTests
{
    [Fact]
    public void StartHop_IsAnimating_ReturnsTrue()
    {
        var animator = new EntityAnimator();
        animator.StartHop("player", Vector2.Zero, Vector2.UnitX, 0.15f);
        Assert.True(animator.IsAnimating("player"));
        Assert.True(animator.HasActiveAnimations);
    }

    [Fact]
    public void Update_PastDuration_RemovesAnimation()
    {
        var animator = new EntityAnimator();
        animator.StartHop("player", Vector2.Zero, Vector2.UnitX, 0.15f);
        animator.Update(0.16f);
        Assert.False(animator.IsAnimating("player"));
        Assert.False(animator.HasActiveAnimations);
    }

    [Fact]
    public void GetRenderPos_NoAnimation_ReturnsNull()
    {
        var animator = new EntityAnimator();
        Assert.Null(animator.GetRenderPos("player"));
    }

    [Fact]
    public void HopMove_MidPoint_HasArcYOffset()
    {
        var animator = new EntityAnimator();
        animator.StartHop("e1", new Vector2(0, 5), new Vector2(1, 5), 1.0f);
        animator.Update(0.5f); // t = 0.5

        var pos = animator.GetRenderPos("e1");
        Assert.NotNull(pos);
        Assert.Equal(0.5f, pos.Value.X, 3); // linear X lerp
        Assert.Equal(5f - EntityAnimator.ArcHeight, pos.Value.Y, 3); // peak of arc
    }

    [Fact]
    public void HopMove_StartAndEnd_NoYOffset()
    {
        var animator = new EntityAnimator();
        // At t=0, position should be exactly From
        animator.StartHop("e1", new Vector2(0, 5), new Vector2(1, 5), 1.0f);
        var startPos = animator.GetRenderPos("e1");
        Assert.Equal(5f, startPos.Value.Y, 3); // no arc offset at t=0
    }

    [Fact]
    public void SlideBack_MidPoint_LinearInterpolation()
    {
        var animator = new EntityAnimator();
        animator.StartSlideBack("e1", new Vector2(0, 0), new Vector2(2, 0));
        animator.Update(EntityAnimator.SlideBackDuration * 0.5f); // t = 0.5

        var pos = animator.GetRenderPos("e1");
        Assert.NotNull(pos);
        Assert.Equal(1f, pos.Value.X, 3); // linear lerp midpoint
        Assert.Equal(0f, pos.Value.Y, 3); // no arc
    }

    [Fact]
    public void SlideBack_CompletesInSlideBackDuration()
    {
        var animator = new EntityAnimator();
        animator.StartSlideBack("e1", Vector2.Zero, Vector2.UnitX);
        animator.Update(EntityAnimator.SlideBackDuration + 0.01f);
        Assert.False(animator.IsAnimating("e1"));
    }

    [Fact]
    public void CompletionCallback_FiresOnFinish()
    {
        var animator = new EntityAnimator();
        bool called = false;
        animator.StartHop("e1", Vector2.Zero, Vector2.UnitX, 0.1f, () => called = true);
        animator.Update(0.11f);
        Assert.True(called);
    }

    [Fact]
    public void ReplacedAnimation_OldCallbackDoesNotFire()
    {
        var animator = new EntityAnimator();
        bool oldCalled = false;
        bool newCalled = false;
        animator.StartHop("e1", Vector2.Zero, Vector2.UnitX, 1.0f, () => oldCalled = true);
        animator.StartHop("e1", Vector2.UnitX, Vector2.One, 0.1f, () => newCalled = true);
        animator.Update(0.11f);
        Assert.False(oldCalled);
        Assert.True(newCalled);
    }

    [Fact]
    public void MultipleSimultaneousAnimations_IndependentProgress()
    {
        var animator = new EntityAnimator();
        animator.StartHop("e1", Vector2.Zero, Vector2.UnitX, 0.1f);
        animator.StartSlideBack("e2", Vector2.Zero, Vector2.UnitY);
        Assert.True(animator.IsAnimating("e1"));
        Assert.True(animator.IsAnimating("e2"));

        animator.Update(EntityAnimator.SlideBackDuration + 0.01f);
        // SlideBack (0.08s) finished, hop (0.1s) still going
        Assert.True(animator.IsAnimating("e1"));
        Assert.False(animator.IsAnimating("e2"));
    }

    [Fact]
    public void Clear_RemovesAllAnimations_NoCallbacksFire()
    {
        var animator = new EntityAnimator();
        bool called = false;
        animator.StartHop("e1", Vector2.Zero, Vector2.UnitX, 1.0f, () => called = true);
        animator.StartSlideBack("e2", Vector2.Zero, Vector2.UnitY);
        animator.Clear();
        Assert.False(animator.HasActiveAnimations);
        Assert.False(called);
    }
}
