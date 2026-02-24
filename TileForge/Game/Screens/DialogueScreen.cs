using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game.Screens;

/// <summary>
/// Overlay screen that displays a branching dialogue conversation. Shows the
/// speaker name, text (with typewriter reveal), and choice options. Evaluates
/// flag conditions and sets flags/variables via GameStateManager.
/// </summary>
public class DialogueScreen : GameScreen
{
    private readonly DialogueData _dialogue;
    private readonly GameStateManager _gameStateManager;

    private DialogueNode _currentNode;
    private List<DialogueChoice> _visibleChoices;
    private int _selectedChoiceIndex;

    // Typewriter state
    private int _revealedChars;
    private float _revealTimer;
    private const float CharsPerSecond = 40f;

    public override bool IsOverlay => true;

    public DialogueScreen(DialogueData dialogue, GameStateManager gameStateManager)
    {
        _dialogue = dialogue;
        _gameStateManager = gameStateManager;
    }

    public override void OnEnter()
    {
        AdvanceToNode(_dialogue.Nodes.FirstOrDefault()?.Id);
    }

    public override void Update(GameTime gameTime, GameInputManager input)
    {
        if (_currentNode == null)
        {
            // Dialogue ended
            ScreenManager.Pop();
            return;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Typewriter reveal
        if (_revealedChars < (_currentNode.Text?.Length ?? 0))
        {
            _revealTimer += dt;
            int charsToReveal = (int)(_revealTimer * CharsPerSecond);
            if (charsToReveal > _revealedChars)
            {
                _revealedChars = charsToReveal;
                if (_revealedChars >= (_currentNode.Text?.Length ?? 0))
                    _revealedChars = _currentNode.Text?.Length ?? 0;
            }

            // Interact during reveal → skip to full text
            if (input.IsActionJustPressed(GameAction.Interact))
            {
                _revealedChars = _currentNode.Text?.Length ?? 0;
                return;
            }
        }
        else
        {
            // Text fully revealed — handle input
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
                    if (!string.IsNullOrEmpty(choice.SetsFlag))
                        _gameStateManager.SetFlag(choice.SetsFlag);
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

        // Check RequiresFlag — skip node if flag not set
        if (!string.IsNullOrEmpty(node.RequiresFlag) && !_gameStateManager.HasFlag(node.RequiresFlag))
        {
            // Skip to NextNodeId (fall through conditional node)
            AdvanceToNode(node.NextNodeId);
            return;
        }

        _currentNode = node;
        _revealedChars = 0;
        _revealTimer = 0f;
        _selectedChoiceIndex = 0;

        // Apply side effects
        if (!string.IsNullOrEmpty(node.SetsFlag))
            _gameStateManager.SetFlag(node.SetsFlag);

        if (!string.IsNullOrEmpty(node.SetsVariable))
        {
            var eqIndex = node.SetsVariable.IndexOf('=');
            if (eqIndex > 0)
            {
                var key = node.SetsVariable.Substring(0, eqIndex);
                var value = node.SetsVariable.Substring(eqIndex + 1);
                _gameStateManager.SetVariable(key, value);
            }
        }

        // Filter visible choices by RequiresFlag
        if (node.Choices != null && node.Choices.Count > 0)
        {
            _visibleChoices = node.Choices
                .Where(c => string.IsNullOrEmpty(c.RequiresFlag) || _gameStateManager.HasFlag(c.RequiresFlag))
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

        // Dialogue box at bottom of screen
        int boxHeight = 120;
        int boxMargin = 8;
        var boxRect = new Rectangle(
            canvasBounds.X + boxMargin,
            canvasBounds.Y + canvasBounds.Height - boxHeight - boxMargin,
            canvasBounds.Width - boxMargin * 2,
            boxHeight);

        // Semi-transparent dark background
        renderer.DrawRect(spriteBatch, boxRect, new Color(0, 0, 0, 200));

        int padding = 8;
        float textX = boxRect.X + padding;
        float textY = boxRect.Y + padding;

        // Speaker name
        if (!string.IsNullOrEmpty(_currentNode.Speaker))
        {
            spriteBatch.DrawString(font, _currentNode.Speaker, new Vector2(textX, textY), Color.Yellow);
            textY += font.MeasureString(_currentNode.Speaker).Y + 4f;
        }

        // Dialogue text (typewriter)
        if (!string.IsNullOrEmpty(_currentNode.Text))
        {
            int chars = _revealedChars < _currentNode.Text.Length ? _revealedChars : _currentNode.Text.Length;
            string visibleText = _currentNode.Text.Substring(0, chars);
            spriteBatch.DrawString(font, visibleText, new Vector2(textX, textY), Color.White);
            textY += font.MeasureString(_currentNode.Text).Y + 8f;
        }

        // Choices
        if (_visibleChoices != null && _revealedChars >= (_currentNode.Text?.Length ?? 0))
        {
            for (int i = 0; i < _visibleChoices.Count; i++)
            {
                string choiceText = _visibleChoices[i].Text ?? "";
                var color = i == _selectedChoiceIndex ? Color.Yellow : Color.LightGray;
                spriteBatch.DrawString(font, $"> {choiceText}", new Vector2(textX + 8, textY), color);
                textY += font.MeasureString(choiceText).Y + 2f;
            }
        }
    }

    /// <summary>
    /// Exposes the current node ID for testing.
    /// </summary>
    internal string CurrentNodeId => _currentNode?.Id;
}
