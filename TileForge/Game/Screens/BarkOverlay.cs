using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game.Screens;

/// <summary>
/// Lightweight floating text bubble that auto-dismisses after a duration.
/// NOT a GameScreen — it does not block input. Called directly by GameplayScreen.
/// </summary>
public class BarkOverlay
{
    private static readonly Random _rng = new();

    private readonly DialogueData _dialogue;
    private readonly GameStateManager _gsm;
    private readonly Vector2 _worldPosition;
    private readonly float _durationSeconds;
    private readonly GameLog _gameLog;

    private DialogueNode _displayNode;
    private float _elapsed;

    public bool IsActive { get; private set; }

    public string DisplayText => IsActive ? _displayNode?.Text : null;

    public BarkOverlay(DialogueData dialogue, GameStateManager gsm, Vector2 worldPosition,
        float durationSeconds = 2f, GameLog gameLog = null)
    {
        _dialogue = dialogue;
        _gsm = gsm;
        _worldPosition = worldPosition;
        _durationSeconds = durationSeconds;
        _gameLog = gameLog;
    }

    /// <summary>
    /// Resolves the display node (via routes or random selection), executes actions, and activates the bark.
    /// </summary>
    public void Start()
    {
        _displayNode = ResolveDisplayNode();
        if (_displayNode == null)
            return;

        _elapsed = 0f;
        IsActive = true;

        // Execute node actions on entry
        ActionExecutor.ExecuteAll(_displayNode.Actions, _gsm, _gameLog);
    }

    /// <summary>
    /// Advances the timer and dismisses when duration is reached.
    /// Sets oneShot flag on dismiss if configured.
    /// </summary>
    public void Update(float deltaSeconds)
    {
        if (!IsActive)
            return;

        _elapsed += deltaSeconds;
        if (_elapsed >= _durationSeconds)
        {
            IsActive = false;

            if (_dialogue.OneShot == true && !string.IsNullOrEmpty(_dialogue.Id))
                _gsm.SetFlag($"dialogue_shown:{_dialogue.Id}");
        }
    }

    /// <summary>
    /// Draws a floating bubble above the entity's world position.
    /// Fades out during the last 25% of the duration.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer, Rectangle canvasBounds)
    {
        if (!IsActive || _displayNode == null)
            return;

        string text = _displayNode.Text ?? string.Empty;
        if (string.IsNullOrEmpty(text))
            return;

        // Calculate fade alpha: full opacity until 75% elapsed, then fade to 0
        float fadeStart = _durationSeconds * 0.75f;
        float alpha = 1f;
        if (_elapsed > fadeStart)
            alpha = 1f - (_elapsed - fadeStart) / (_durationSeconds * 0.25f);
        alpha = Math.Clamp(alpha, 0f, 1f);

        byte a = (byte)(alpha * 255);

        // Measure text for bubble sizing
        Vector2 textSize = font.MeasureString(text);
        int padding = 6;
        int bubbleWidth = (int)textSize.X + padding * 2;
        int bubbleHeight = (int)textSize.Y + padding * 2;

        // Position the bubble above the world position (treat worldPosition as screen coords here)
        int bubbleX = (int)_worldPosition.X - bubbleWidth / 2;
        int bubbleY = (int)_worldPosition.Y - bubbleHeight - 8;

        // Clamp to canvas bounds
        bubbleX = Math.Clamp(bubbleX, canvasBounds.X, canvasBounds.Right - bubbleWidth);
        bubbleY = Math.Clamp(bubbleY, canvasBounds.Y, canvasBounds.Bottom - bubbleHeight);

        var bgRect = new Rectangle(bubbleX, bubbleY, bubbleWidth, bubbleHeight);
        byte bgAlpha = (byte)(a * 0.85f);
        renderer.DrawRect(spriteBatch, bgRect, new Color((byte)0, (byte)0, (byte)0, bgAlpha));

        // Draw text
        spriteBatch.DrawString(font, text,
            new Vector2(bubbleX + padding, bubbleY + padding),
            new Color((byte)255, (byte)255, (byte)255, a));
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    private DialogueNode ResolveDisplayNode()
    {
        // 1. Try route evaluation
        var startId = _dialogue.ResolveStartNodeId(_gsm);
        if (startId != null)
        {
            var routeNode = _dialogue.Nodes.FirstOrDefault(n => n.Id == startId);
            if (routeNode != null)
                return routeNode;
        }

        // 2. Random selection: filter nodes tagged "random" whose conditions pass
        var randomNodes = _dialogue.Nodes
            .Where(n => n.Tags != null && n.Tags.Contains("random"))
            .Where(n => ConditionEvaluator.EvaluateAll(n.Conditions, _gsm))
            .ToList();

        if (randomNodes.Count > 0)
            return randomNodes[_rng.Next(randomNodes.Count)];

        // 3. Fallback: first node
        return _dialogue.Nodes.FirstOrDefault();
    }
}
