using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game.Screens;

/// <summary>
/// Overlay screen that displays a branching dialogue conversation. Shows the
/// speaker name, text (with typewriter reveal), and choice options. Evaluates
/// conditions and executes actions via ConditionEvaluator/ActionExecutor.
/// </summary>
public class DialogueScreen : GameScreen
{
    private readonly DialogueData _dialogue;
    private readonly GameStateManager _gameStateManager;
    private readonly GameLog _gameLog;

    private DialogueNode _currentNode;
    private List<DialogueChoice> _visibleChoices;
    private int _selectedChoiceIndex;

    // Typewriter state
    private int _revealedChars;
    private float _revealTimer;
    private const float CharsPerSecond = 40f;

    public override bool IsOverlay => true;

    public DialogueScreen(DialogueData dialogue, GameStateManager gameStateManager,
        GameLog gameLog = null)
    {
        _dialogue = dialogue;
        _gameStateManager = gameStateManager;
        _gameLog = gameLog;
    }

    public override void OnEnter()
    {
        AdvanceToNode(_dialogue.ResolveStartNodeId(_gameStateManager));
    }

    public override void Update(GameTime gameTime, GameInputManager input)
    {
        if (_currentNode == null)
        {
            // Dialogue ended — handle oneShot
            if (_dialogue.OneShot == true && !string.IsNullOrEmpty(_dialogue.Id))
                _gameStateManager.SetFlag($"dialogue_shown:{_dialogue.Id}");
            ScreenManager.Pop();
            return;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Typewriter reveal
        int textLen = _currentNode.Text?.Length ?? 0;
        if (_revealedChars < textLen)
        {
            _revealTimer += dt;
            int charsToReveal = (int)(_revealTimer * CharsPerSecond);
            if (charsToReveal > _revealedChars)
            {
                _revealedChars = charsToReveal;
                if (_revealedChars >= textLen)
                    _revealedChars = textLen;
            }

            // Interact during reveal -> skip to full text
            if (input.IsActionJustPressed(GameAction.Interact))
            {
                _revealedChars = textLen;
                return;
            }
        }
        else
        {
            // Text fully revealed -- handle input
            if (_visibleChoices != null && _visibleChoices.Count > 0)
            {
                // Branching: navigate choices
                if (input.IsActionJustPressed(GameAction.MoveUp))
                {
                    _selectedChoiceIndex--;
                    if (_selectedChoiceIndex < 0) _selectedChoiceIndex = _visibleChoices.Count - 1;
                }
                if (input.IsActionJustPressed(GameAction.MoveDown))
                {
                    _selectedChoiceIndex++;
                    if (_selectedChoiceIndex >= _visibleChoices.Count) _selectedChoiceIndex = 0;
                }
                if (input.IsActionJustPressed(GameAction.Interact))
                {
                    var choice = _visibleChoices[_selectedChoiceIndex];
                    // Execute choice actions
                    ActionExecutor.ExecuteAll(choice.Actions, _gameStateManager, _gameLog);
                    AdvanceToNode(choice.NextNodeId);
                }
            }
            else
            {
                // Linear: advance on Interact
                if (input.IsActionJustPressed(GameAction.Interact))
                {
                    AdvanceToNode(_currentNode.NextNodeId);
                }
            }
        }

        // Cancel exits dialogue
        if (input.IsActionJustPressed(GameAction.Cancel))
        {
            ScreenManager.Pop();
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
        if (!ConditionEvaluator.EvaluateAll(node.Conditions, _gameStateManager))
        {
            AdvanceToNode(node.NextNodeId);
            return;
        }

        _currentNode = node;
        _revealedChars = 0;
        _revealTimer = 0f;
        _selectedChoiceIndex = 0;

        // Log dialogue to the persistent game log
        if (_gameLog != null && !string.IsNullOrEmpty(node.Text))
        {
            string speaker = node.Speaker ?? "???";
            _gameLog.Add($"{speaker}: {node.Text}", new Color(140, 200, 220));
        }

        // Execute actions
        ActionExecutor.ExecuteAll(node.Actions, _gameStateManager, _gameLog);

        // Filter visible choices by conditions
        if (node.Choices != null && node.Choices.Count > 0)
        {
            _visibleChoices = node.Choices
                .Where(c => ConditionEvaluator.EvaluateAll(c.Conditions, _gameStateManager))
                .ToList();
        }
        else
        {
            _visibleChoices = null;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, SpriteFont font,
        Renderer renderer, Rectangle canvasBounds)
    {
        if (_currentNode == null) return;

        int boxMargin = 8;
        int padding = 8;
        int maxTextWidth = canvasBounds.Width - boxMargin * 2 - padding * 2;
        float lineHeight = font.MeasureString("A").Y;

        // Pre-compute wrapped lines to size the box
        var wrappedLines = new List<string>();
        if (!string.IsNullOrEmpty(_currentNode.Text))
            wrappedLines = TextUtils.WrapText(font, _currentNode.Text, maxTextWidth);

        float contentHeight = padding * 2;
        if (!string.IsNullOrEmpty(_currentNode.Speaker))
            contentHeight += lineHeight + 4f;
        if (wrappedLines.Count > 0)
            contentHeight += wrappedLines.Count * lineHeight + 8f;
        if (_visibleChoices != null && _revealedChars >= (_currentNode.Text?.Length ?? 0))
            contentHeight += _visibleChoices.Count * (lineHeight + 2f);

        int boxHeight = (int)System.Math.Max(contentHeight, 60);
        var boxRect = new Rectangle(
            canvasBounds.X + boxMargin,
            canvasBounds.Y + canvasBounds.Height - boxHeight - boxMargin,
            canvasBounds.Width - boxMargin * 2,
            boxHeight);

        // Semi-transparent dark background
        renderer.DrawRect(spriteBatch, boxRect, new Color(0, 0, 0, 200));

        float textX = boxRect.X + padding;
        float textY = boxRect.Y + padding;

        // Speaker name
        if (!string.IsNullOrEmpty(_currentNode.Speaker))
        {
            spriteBatch.DrawString(font, _currentNode.Speaker, new Vector2(textX, textY), Color.Yellow);
            textY += lineHeight + 4f;
        }

        // Dialogue text (typewriter with word-wrap)
        if (wrappedLines.Count > 0)
        {
            int chars = System.Math.Min(_revealedChars, _currentNode.Text.Length);
            var revealedLines = chars >= _currentNode.Text.Length
                ? wrappedLines
                : TextUtils.WrapText(font, _currentNode.Text.Substring(0, chars), maxTextWidth);

            foreach (var line in revealedLines)
            {
                spriteBatch.DrawString(font, line, new Vector2(textX, textY), Color.White);
                textY += lineHeight;
            }
            // Reserve space for full text so box doesn't resize during reveal
            textY += (wrappedLines.Count - revealedLines.Count) * lineHeight + 8f;
        }

        // Choices
        if (_visibleChoices != null && _revealedChars >= (_currentNode.Text?.Length ?? 0))
        {
            for (int i = 0; i < _visibleChoices.Count; i++)
            {
                string choiceText = _visibleChoices[i].Text ?? "";
                var color = i == _selectedChoiceIndex ? Color.Yellow : Color.LightGray;
                spriteBatch.DrawString(font, $"> {choiceText}", new Vector2(textX + 8, textY), color);
                textY += lineHeight + 2f;
            }
        }
    }

    /// <summary>
    /// Exposes the current node ID for testing.
    /// </summary>
    internal string CurrentNodeId => _currentNode?.Id;
}
