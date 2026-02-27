using TileForge.Game;

namespace TileForge.Infrastructure;

/// <summary>
/// Loads dialogue data by reference ID.
/// Abstracts file-based dialogue loading for testability.
/// </summary>
public interface IDialogueLoader
{
    /// <summary>
    /// Loads dialogue data for the given reference (e.g. "elder_greeting").
    /// Returns null if not found or on error.
    /// </summary>
    DialogueData LoadDialogue(string dialogueRef);
}
