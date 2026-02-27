using System.Collections.Generic;
using TileForge.Game;
using TileForge.Infrastructure;

namespace TileForge.Tests.Helpers;

/// <summary>
/// In-memory mock for IDialogueLoader. Returns pre-configured dialogues by reference ID.
/// </summary>
public class MockDialogueLoader : IDialogueLoader
{
    public Dictionary<string, DialogueData> Dialogues { get; } = new();

    public DialogueData LoadDialogue(string dialogueRef)
    {
        return Dialogues.TryGetValue(dialogueRef, out var data) ? data : null;
    }
}
