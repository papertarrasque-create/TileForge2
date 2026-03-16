using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game.Screens;

/// <summary>
/// Overlay screen for signs, books, item descriptions, and trigger tiles.
/// Compact centered popup; blocks input. Multi-node support allows books with
/// multiple pages. Interact advances pages; Cancel always closes.
/// </summary>
public class InspectOverlay : GameScreen
{
    private readonly DialogueData _dialogue;
    private readonly GameStateManager _gsm;
    private readonly GameLog _gameLog;

    private DialogueNode _currentNode;

    public override bool IsOverlay => true;

    /// <summary>Exposes the current node text for testing.</summary>
    public string DisplayText => _currentNode?.Text;

    public InspectOverlay(DialogueData dialogue, GameStateManager gsm, GameLog gameLog = null)
    {
        _dialogue = dialogue;
        _gsm = gsm;
        _gameLog = gameLog;
    }

    public override void OnEnter()
    {
        string startNodeId = ResolveStartNode();
        AdvanceToNode(startNodeId);
    }

    /// <summary>
    /// Evaluates routes top-to-bottom, returning the first matching startNode.
    /// Falls back to first node if no routes defined.
    /// </summary>
    private string ResolveStartNode()
    {
        if (_dialogue.Routes != null)
        {
            foreach (var route in _dialogue.Routes)
            {
                if (ConditionEvaluator.EvaluateAll(route.Conditions, _gsm))
                    return route.StartNode;
            }
        }
        return _dialogue.Nodes.FirstOrDefault()?.Id;
    }

    /// <summary>
    /// Moves to the node with the given ID. Evaluates conditions; if they fail
    /// the node is skipped to its NextNodeId. Executes actions on entry.
    /// </summary>
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

        // Check v2 conditions -- skip if not met
        if (!ConditionEvaluator.EvaluateAll(node.Conditions, _gsm))
        {
            AdvanceToNode(node.NextNodeId);
            return;
        }

        // v1 compat: RequiresFlag
        if (!string.IsNullOrEmpty(node.RequiresFlag) && !_gsm.HasFlag(node.RequiresFlag))
        {
            AdvanceToNode(node.NextNodeId);
            return;
        }

        _currentNode = node;

        // Execute actions on node entry
        ActionExecutor.ExecuteAll(node.Actions, _gsm, _gameLog);

        // v1 compat: SetsFlag / SetsVariable
        if (!string.IsNullOrEmpty(node.SetsFlag))
            _gsm.SetFlag(node.SetsFlag);

        if (!string.IsNullOrEmpty(node.SetsVariable))
        {
            var eqIndex = node.SetsVariable.IndexOf('=');
            if (eqIndex > 0)
            {
                var key = node.SetsVariable.Substring(0, eqIndex);
                var value = node.SetsVariable.Substring(eqIndex + 1);
                _gsm.SetVariable(key, value);
            }
        }
    }

    public override void Update(GameTime gameTime, GameInputManager input)
    {
        // Cancel always dismisses
        if (input.IsActionJustPressed(GameAction.Cancel))
        {
            Dismiss();
            return;
        }

        // Interact: advance to next page, or dismiss if no next page
        if (input.IsActionJustPressed(GameAction.Interact))
        {
            if (_currentNode != null && !string.IsNullOrEmpty(_currentNode.NextNodeId))
            {
                AdvanceToNode(_currentNode.NextNodeId);
            }
            else
            {
                Dismiss();
            }
        }
    }

    /// <summary>
    /// Sets oneShot flag if applicable and pops this screen from the manager.
    /// </summary>
    private void Dismiss()
    {
        if (_dialogue.OneShot == true && !string.IsNullOrEmpty(_dialogue.Id))
            _gsm.SetFlag($"dialogue_shown:{_dialogue.Id}");

        ScreenManager.Pop();
    }

    public override void Draw(SpriteBatch spriteBatch, SpriteFont font,
        Renderer renderer, Rectangle canvasBounds)
    {
        if (_currentNode == null) return;

        // Centered box -- narrower and shorter than DialogueScreen
        int boxWidth = (int)(canvasBounds.Width * 0.6f);
        int boxHeight = 100;
        int boxX = canvasBounds.X + (canvasBounds.Width - boxWidth) / 2;
        int boxY = canvasBounds.Y + (canvasBounds.Height - boxHeight) / 2;
        var boxRect = new Rectangle(boxX, boxY, boxWidth, boxHeight);

        // Semi-transparent dark background
        renderer.DrawRect(spriteBatch, boxRect, new Color(0, 0, 0, 210));

        int padding = 10;
        float textX = boxRect.X + padding;
        float textY = boxRect.Y + padding;

        // Speaker name in yellow (used as title for books/signs)
        if (!string.IsNullOrEmpty(_currentNode.Speaker))
        {
            spriteBatch.DrawString(font, _currentNode.Speaker, new Vector2(textX, textY), Color.Yellow);
            textY += font.MeasureString(_currentNode.Speaker).Y + 4f;
        }

        // Node text in white
        if (!string.IsNullOrEmpty(_currentNode.Text))
        {
            spriteBatch.DrawString(font, _currentNode.Text, new Vector2(textX, textY), Color.White);
            textY += font.MeasureString(_currentNode.Text).Y + 6f;
        }

        // Hint text
        string hint = !string.IsNullOrEmpty(_currentNode.NextNodeId)
            ? "[Z] Next  [X] Close"
            : "[Z/X] Close";
        spriteBatch.DrawString(font, hint, new Vector2(textX, textY), Color.Gray);
    }
}
