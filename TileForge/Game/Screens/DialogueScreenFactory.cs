using Microsoft.Xna.Framework;

namespace TileForge.Game.Screens;

public class DialogueScreenResult
{
    public GameScreen Screen { get; init; }
    public BarkOverlay BarkOverlay { get; init; }
}

public static class DialogueScreenFactory
{
    public static DialogueScreenResult Create(DialogueData dialogue, GameStateManager gsm,
        GameLog gameLog = null, Vector2? entityWorldPos = null)
    {
        return (dialogue.Type ?? "conversation") switch
        {
            "bark" => new DialogueScreenResult
            {
                BarkOverlay = new BarkOverlay(dialogue, gsm,
                    entityWorldPos ?? Vector2.Zero, gameLog: gameLog),
            },
            "inspect" => new DialogueScreenResult
            {
                Screen = new InspectOverlay(dialogue, gsm, gameLog),
            },
            "cutscene" => new DialogueScreenResult
            {
                Screen = new CutsceneScreen(dialogue, gsm, gameLog: gameLog),
            },
            _ => new DialogueScreenResult
            {
                Screen = new DialogueScreen(dialogue, gsm, gameLog),
            },
        };
    }
}
