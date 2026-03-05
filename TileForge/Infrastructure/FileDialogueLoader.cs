using System.IO;
using System.Text.Json;
using TileForge.Game;

namespace TileForge.Infrastructure;

/// <summary>
/// Loads dialogue files from the project directory.
/// Tries {basePath}/{ref}.json first, then {basePath}/dialogues/{ref}.json.
/// </summary>
public class FileDialogueLoader : IDialogueLoader
{
    private readonly IFileSystem _fileSystem;
    private readonly string _basePath;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public FileDialogueLoader(string basePath, IFileSystem fileSystem = null)
    {
        _basePath = basePath;
        _fileSystem = fileSystem ?? new DefaultFileSystem();
    }

    public DialogueData LoadDialogue(string dialogueRef)
    {
        if (string.IsNullOrEmpty(_basePath))
            return null;

        string path = Path.Combine(_basePath, dialogueRef + ".json");
        if (!_fileSystem.Exists(path))
            path = Path.Combine(_basePath, "dialogues", dialogueRef + ".json");

        if (!_fileSystem.Exists(path))
            return null;

        try
        {
            string json = _fileSystem.ReadAllText(path);
            return JsonSerializer.Deserialize<DialogueData>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
