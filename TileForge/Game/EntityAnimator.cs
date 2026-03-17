using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileForge.Game;

public enum AnimationType { HopMove, SlideBack }

public class EntityAnimator
{
    public const float DefaultHopDuration = 0.15f;
    public const float SlideBackDuration = 0.08f;
    public const float ArcHeight = 0.3f;

    private class Animation
    {
        public AnimationType Type;
        public Vector2 From;
        public Vector2 To;
        public float Duration;
        public float Progress;
        public Action OnComplete;
    }

    private readonly Dictionary<string, Animation> _active = new();

    public bool HasActiveAnimations => _active.Count > 0;

    public void StartHop(string id, Vector2 from, Vector2 to, float duration, Action onComplete = null)
    {
        _active[id] = new Animation
        {
            Type = AnimationType.HopMove,
            From = from,
            To = to,
            Duration = Math.Max(duration, 0.001f),
            Progress = 0f,
            OnComplete = onComplete,
        };
    }

    public void StartSlideBack(string id, Vector2 from, Vector2 to, Action onComplete = null)
    {
        _active[id] = new Animation
        {
            Type = AnimationType.SlideBack,
            From = from,
            To = to,
            Duration = SlideBackDuration,
            Progress = 0f,
            OnComplete = onComplete,
        };
    }

    public void Update(float dt)
    {
        // Collect completed animations to process after iteration
        List<Action> completions = null;

        var toRemove = new List<string>();
        foreach (var (id, anim) in _active)
        {
            anim.Progress += dt / anim.Duration;
            if (anim.Progress >= 1.0f)
            {
                toRemove.Add(id);
                if (anim.OnComplete != null)
                {
                    completions ??= new List<Action>();
                    completions.Add(anim.OnComplete);
                }
            }
        }

        foreach (var id in toRemove)
            _active.Remove(id);

        // Fire callbacks after removal (callbacks may start new animations)
        if (completions != null)
        {
            foreach (var cb in completions)
                cb();
        }
    }

    public bool IsAnimating(string id) => _active.ContainsKey(id);

    public Vector2? GetRenderPos(string id)
    {
        if (!_active.TryGetValue(id, out var anim))
            return null;

        float t = Math.Clamp(anim.Progress, 0f, 1f);
        var pos = Vector2.Lerp(anim.From, anim.To, t);

        if (anim.Type == AnimationType.HopMove)
        {
            // Parabolic arc: peaks at t=0.5 with offset -ArcHeight (upward in screen space)
            pos.Y += -ArcHeight * 4f * t * (1f - t);
        }

        return pos;
    }

    /// <summary>
    /// Returns the linear interpolation position without arc offset.
    /// Used for camera follow so it tracks horizontal movement without vertical bounce.
    /// </summary>
    public Vector2? GetGroundPos(string id)
    {
        if (!_active.TryGetValue(id, out var anim))
            return null;

        float t = Math.Clamp(anim.Progress, 0f, 1f);
        return Vector2.Lerp(anim.From, anim.To, t);
    }

    public void Clear()
    {
        _active.Clear();
    }
}
