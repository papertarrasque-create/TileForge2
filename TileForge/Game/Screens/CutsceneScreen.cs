using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game.Screens;

/// <summary>
/// Full-screen overlay screen for intro sequences, dream sequences, and story beats.
/// Nodes auto-advance on a timer (default 3s) or player can press Interact to advance manually.
/// Cancel skips the whole cutscene immediately. Actions fire on each node entry.
/// Per-node "auto_advance_ms:N" tag allows different pacing per node.
/// </summary>
public class CutsceneScreen : GameScreen
{
    private readonly DialogueData _dialogue;
    private readonly GameStateManager _gsm;
    private readonly float _defaultAutoAdvance;
    private readonly GameLog _gameLog;

    private DialogueNode _currentNode;
    private float _nodeElapsed;

    public override bool IsOverlay => true;

    /// <summary>
    /// Exposes the current node's text for testing.
    /// </summary>
    public string DisplayText => _currentNode?.Text;

    public CutsceneScreen(DialogueData dialogue, GameStateManager gsm,
        float autoAdvanceSeconds = 3f, GameLog gameLog = null)
    {
        _dialogue = dialogue;
        _gsm = gsm;
        _defaultAutoAdvance = autoAdvanceSeconds;
        _gameLog = gameLog;
    }

    public override void OnEnter()
    {
        AdvanceToNode(_dialogue.ResolveStartNodeId(_gsm));
    }

    public override void Update(GameTime gameTime, GameInputManager input)
    {
        // Cancel exits the entire cutscene immediately
        if (input.IsActionJustPressed(GameAction.Cancel))
        {
            EndCutscene();
            return;
        }

        if (_currentNode == null)
        {
            EndCutscene();
            return;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _nodeElapsed += dt;

        // Manual advance via Interact
        if (input.IsActionJustPressed(GameAction.Interact))
        {
            AdvanceToNode(_currentNode.NextNodeId);
            return;
        }

        // Auto-advance when elapsed time reaches the threshold
        if (_nodeElapsed >= GetAutoAdvanceTime())
        {
            AdvanceToNode(_currentNode.NextNodeId);
        }
    }

    private void AdvanceToNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            _currentNode = null;
            return;
        }

        var node = _dialogue.Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node == null)
        {
            _currentNode = null;
            return;
        }

        // Check conditions — skip node if conditions not met
        if (!ConditionEvaluator.EvaluateAll(node.Conditions, _gsm))
        {
            AdvanceToNode(node.NextNodeId);
            return;
        }

        _currentNode = node;
        _nodeElapsed = 0f;

        // Execute node actions on entry
        ActionExecutor.ExecuteAll(node.Actions, _gsm, _gameLog);
    }

    /// <summary>
    /// Returns the auto-advance delay for the current node.
    /// Checks node Tags for "auto_advance_ms:N", parses N as int ms to float seconds.
    /// Falls back to _defaultAutoAdvance if no tag is found.
    /// </summary>
    private float GetAutoAdvanceTime()
    {
        if (_currentNode?.Tags != null)
        {
            const string prefix = "auto_advance_ms:";
            foreach (var tag in _currentNode.Tags)
            {
                if (tag.StartsWith(prefix))
                {
                    var msStr = tag.Substring(prefix.Length);
                    if (int.TryParse(msStr, out int ms))
                        return ms / 1000f;
                }
            }
        }
        return _defaultAutoAdvance;
    }

    private void EndCutscene()
    {
        if (_dialogue.OneShot == true && !string.IsNullOrEmpty(_dialogue.Id))
            _gsm.SetFlag($"dialogue_shown:{_dialogue.Id}");

        ScreenManager.Pop();
    }

    public override void Draw(SpriteBatch spriteBatch, SpriteFont font,
        Renderer renderer, Rectangle canvasBounds)
    {
        if (_currentNode == null) return;

        // Full-screen dark overlay
        renderer.DrawRect(spriteBatch, canvasBounds, new Color(0, 0, 0, 220));

        // Center the text vertically and horizontally
        int cx = canvasBounds.X + canvasBounds.Width / 2;
        int cy = canvasBounds.Y + canvasBounds.Height / 2;

        // Speaker name (centered, yellow)
        if (!string.IsNullOrEmpty(_currentNode.Speaker))
        {
            var speakerSize = font.MeasureString(_currentNode.Speaker);
            spriteBatch.DrawString(font, _currentNode.Speaker,
                new Vector2(cx - speakerSize.X / 2f, cy - speakerSize.Y - 8f),
                Color.Yellow);
        }

        // Body text (centered, white)
        if (!string.IsNullOrEmpty(_currentNode.Text))
        {
            var textSize = font.MeasureString(_currentNode.Text);
            spriteBatch.DrawString(font, _currentNode.Text,
                new Vector2(cx - textSize.X / 2f, cy),
                Color.White);
        }

        // Hint at bottom
        const string hint = "[E] Continue  [Esc] Skip";
        var hintSize = font.MeasureString(hint);
        int hintY = canvasBounds.Bottom - (int)hintSize.Y - 12;
        spriteBatch.DrawString(font, hint,
            new Vector2(cx - hintSize.X / 2f, hintY),
            Color.Gray);
    }
}
