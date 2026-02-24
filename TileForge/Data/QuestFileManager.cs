using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TileForge.Game;

namespace TileForge.Data;

/// <summary>
/// Loads and saves quest definitions to quests.json in the project directory.
/// Reading delegates to QuestLoader. Writing uses snake_case JSON to match the existing format.
/// </summary>
public static class QuestFileManager
{
    private const string QuestFileName = "quests.json";

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string GetQuestPath(string projectDir) =>
        Path.Combine(projectDir, QuestFileName);

    /// <summary>
    /// Loads quest definitions from {projectDir}/quests.json.
    /// Returns an empty list if the file is missing or invalid.
    /// </summary>
    public static List<QuestDefinition> Load(string projectDir)
    {
        if (string.IsNullOrEmpty(projectDir))
            return new List<QuestDefinition>();

        return QuestLoader.Load(GetQuestPath(projectDir));
    }

    /// <summary>
    /// Saves quest definitions to {projectDir}/quests.json in snake_case format.
    /// </summary>
    public static void Save(string projectDir, List<QuestDefinition> quests)
    {
        if (string.IsNullOrEmpty(projectDir)) return;

        string json = ToJson(quests);
        File.WriteAllText(GetQuestPath(projectDir), json);
    }

    /// <summary>
    /// Serializes quest definitions to JSON. Testable without filesystem.
    /// </summary>
    public static string ToJson(List<QuestDefinition> quests)
    {
        var file = new QuestFile { Quests = quests ?? new List<QuestDefinition>() };
        return JsonSerializer.Serialize(file, WriteOptions);
    }
}
