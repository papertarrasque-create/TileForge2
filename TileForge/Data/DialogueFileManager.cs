using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TileForge.Game;

namespace TileForge.Data;

/// <summary>
/// Loads and saves dialogue definitions to per-file JSON in the project's dialogues/ directory.
/// Writing uses camelCase JSON to match the existing dialogue format (nextNodeId, setsFlag, etc.).
/// Reading uses case-insensitive property matching to match how GameplayScreen.LoadDialogue reads dialogues.
/// </summary>
public static class DialogueFileManager
{
    private const string DialoguesDirName = "dialogues";

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static string GetDialoguesDir(string projectDir) =>
        Path.Combine(projectDir, DialoguesDirName);

    /// <summary>
    /// Loads all dialogue definitions from {projectDir}/dialogues/*.json.
    /// Returns an empty list if the directory is missing or projectDir is null/empty.
    /// Silently skips malformed files.
    /// </summary>
    public static List<DialogueData> LoadAll(string projectDir)
    {
        if (string.IsNullOrEmpty(projectDir))
            return new List<DialogueData>();

        string dir = GetDialoguesDir(projectDir);
        if (!Directory.Exists(dir))
            return new List<DialogueData>();

        var result = new List<DialogueData>();
        foreach (string file in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                string json = File.ReadAllText(file);
                var dialogue = JsonSerializer.Deserialize<DialogueData>(json, ReadOptions);
                if (dialogue != null)
                    result.Add(dialogue);
            }
            catch
            {
                // Silently skip malformed files
            }
        }
        return result;
    }

    /// <summary>
    /// Saves a single dialogue definition to {projectDir}/dialogues/{dialogue.Id}.json.
    /// Creates the dialogues/ directory if it does not exist.
    /// </summary>
    public static void SaveOne(string projectDir, DialogueData dialogue)
    {
        if (string.IsNullOrEmpty(projectDir)) return;

        string dir = GetDialoguesDir(projectDir);
        Directory.CreateDirectory(dir);

        string path = Path.Combine(dir, $"{dialogue.Id}.json");
        File.WriteAllText(path, ToJson(dialogue));
    }

    /// <summary>
    /// Deletes {projectDir}/dialogues/{dialogueId}.json if it exists.
    /// </summary>
    public static void DeleteOne(string projectDir, string dialogueId)
    {
        if (string.IsNullOrEmpty(projectDir)) return;

        string path = Path.Combine(GetDialoguesDir(projectDir), $"{dialogueId}.json");
        if (File.Exists(path))
            File.Delete(path);
    }

    /// <summary>
    /// Serializes a dialogue definition to JSON. Testable without filesystem.
    /// </summary>
    public static string ToJson(DialogueData dialogue)
    {
        var data = dialogue ?? new DialogueData();
        return JsonSerializer.Serialize(data, WriteOptions);
    }
}
