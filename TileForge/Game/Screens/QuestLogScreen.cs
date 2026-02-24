using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game.Screens;

/// <summary>
/// Overlay screen showing the player's quest log. Lists active quests with
/// objective checklists and completed quests. View-only — no interact action.
/// Close with Cancel or OpenQuestLog toggle.
/// </summary>
public class QuestLogScreen : GameScreen
{
    private readonly QuestManager _questManager;
    private readonly GameStateManager _gameStateManager;
    private int _scrollOffset;

    public override bool IsOverlay => true;

    public QuestLogScreen(QuestManager questManager, GameStateManager gameStateManager)
    {
        _questManager = questManager;
        _gameStateManager = gameStateManager;
    }

    public override void Update(GameTime gameTime, GameInputManager input)
    {
        if (input.IsActionJustPressed(GameAction.MoveUp))
            _scrollOffset = System.Math.Max(0, _scrollOffset - 1);
        if (input.IsActionJustPressed(GameAction.MoveDown))
            _scrollOffset++;

        if (input.IsActionJustPressed(GameAction.Cancel) ||
            input.IsActionJustPressed(GameAction.OpenQuestLog))
        {
            ScreenManager.Pop();
        }
    }

    public override void Draw(SpriteBatch spriteBatch, SpriteFont font,
        Renderer renderer, Rectangle canvasBounds)
    {
        // Dark overlay
        renderer.DrawRect(spriteBatch, canvasBounds, new Color(0, 0, 0, 180));

        // Title
        var titleText = "QUEST LOG";
        var titleSize = font.MeasureString(titleText);
        var titlePos = new Vector2(
            canvasBounds.X + (canvasBounds.Width - titleSize.X) / 2f,
            canvasBounds.Y + 40f);
        spriteBatch.DrawString(font, titleText, titlePos, Color.White);

        float y = titlePos.Y + titleSize.Y + 20f;
        float leftMargin = canvasBounds.X + 40f;
        float indentMargin = leftMargin + 20f;
        float lineHeight = font.LineSpacing + 2f;

        // Separate active and completed quests
        var activeQuests = new List<QuestDefinition>();
        var completedQuests = new List<QuestDefinition>();

        foreach (var quest in _questManager.Quests)
        {
            var status = _questManager.GetQuestStatus(quest, _gameStateManager);
            if (status == QuestStatus.Active)
                activeQuests.Add(quest);
            else if (status == QuestStatus.Completed)
                completedQuests.Add(quest);
        }

        if (activeQuests.Count == 0 && completedQuests.Count == 0)
        {
            var emptyText = "No quests";
            var emptySize = font.MeasureString(emptyText);
            spriteBatch.DrawString(font, emptyText,
                new Vector2(canvasBounds.X + (canvasBounds.Width - emptySize.X) / 2f, y),
                Color.Gray);
            return;
        }

        // Active quests
        foreach (var quest in activeQuests)
        {
            spriteBatch.DrawString(font, quest.Name, new Vector2(leftMargin, y), Color.White);
            y += lineHeight;

            if (!string.IsNullOrEmpty(quest.Description))
            {
                spriteBatch.DrawString(font, quest.Description,
                    new Vector2(indentMargin, y), Color.LightGray);
                y += lineHeight;
            }

            foreach (var obj in quest.Objectives)
            {
                bool met = QuestManager.EvaluateObjective(obj, _gameStateManager);
                string marker = met ? "[x]" : "[ ]";
                Color color = met ? Color.LimeGreen : Color.Gray;
                spriteBatch.DrawString(font, $"{marker} {obj.Description}",
                    new Vector2(indentMargin, y), color);
                y += lineHeight;
            }

            y += 6f;
        }

        // Completed quests
        if (completedQuests.Count > 0)
        {
            y += 8f;
            spriteBatch.DrawString(font, "-- Completed --",
                new Vector2(leftMargin, y), Color.DarkGray);
            y += lineHeight + 4f;

            foreach (var quest in completedQuests)
            {
                spriteBatch.DrawString(font, $"{quest.Name} (Complete)",
                    new Vector2(leftMargin, y), Color.DarkGray);
                y += lineHeight;
            }
        }
    }
}
