using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TileForge.Infrastructure;

namespace TileForge.Game;

/// <summary>
/// Loads quest definitions from a JSON file. Follows the same pattern as
/// dialogue loading (file-based, case-insensitive deserialization).
/// Supports both snake_case ("start_flag") and PascalCase ("StartFlag") JSON keys.
/// </summary>
public static class QuestLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new NormalizedQuestFileConverter() },
    };

    /// <summary>
    /// Loads all quest definitions from a JSON file.
    /// Returns an empty list if the file doesn't exist or is invalid.
    /// </summary>
    public static List<QuestDefinition> Load(string path)
    {
        return Load(path, new DefaultFileSystem());
    }

    /// <summary>
    /// Loads all quest definitions from a JSON file using the provided filesystem.
    /// Returns an empty list if the file doesn't exist or is invalid.
    /// </summary>
    public static List<QuestDefinition> Load(string path, IFileSystem fileSystem)
    {
        if (string.IsNullOrEmpty(path) || !fileSystem.Exists(path))
            return new List<QuestDefinition>();

        try
        {
            string json = fileSystem.ReadAllText(path);
            return LoadFromJson(json);
        }
        catch
        {
            return new List<QuestDefinition>();
        }
    }

    /// <summary>
    /// Deserializes quest definitions from a JSON string. Testable without filesystem.
    /// Returns an empty list for null or empty input. Throws JsonException for malformed JSON.
    /// Supports both snake_case ("start_flag") and PascalCase ("StartFlag") JSON keys.
    /// </summary>
    public static List<QuestDefinition> LoadFromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new List<QuestDefinition>();

        var file = JsonSerializer.Deserialize<QuestFile>(json, Options);
        return file?.Quests ?? new List<QuestDefinition>();
    }

    // -------------------------------------------------------------------------
    // Custom converter: normalize JSON property names by stripping underscores
    // and converting to lowercase before matching C# property names.
    // This allows both "start_flag" and "StartFlag" to map to StartFlag.
    // -------------------------------------------------------------------------

    private static string Normalize(string key) =>
        key.Replace("_", "").ToLowerInvariant();

    private sealed class NormalizedQuestFileConverter : JsonConverter<QuestFile>
    {
        public override QuestFile Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var file = new QuestFile();

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object for QuestFile.");

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) break;
                if (reader.TokenType != JsonTokenType.PropertyName) continue;

                string propName = reader.GetString();
                reader.Read();

                if (Normalize(propName) == "quests")
                {
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (reader.TokenType == JsonTokenType.StartObject)
                                file.Quests.Add(ReadQuestDefinition(ref reader));
                        }
                    }
                }
                else
                {
                    reader.Skip();
                }
            }

            return file;
        }

        public override void Write(Utf8JsonWriter writer, QuestFile value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value, new JsonSerializerOptions());

        private static QuestDefinition ReadQuestDefinition(ref Utf8JsonReader reader)
        {
            var quest = new QuestDefinition();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) break;
                if (reader.TokenType != JsonTokenType.PropertyName) continue;

                string propName = reader.GetString();
                reader.Read();
                string norm = Normalize(propName);

                switch (norm)
                {
                    case "id":          quest.Id = reader.GetString(); break;
                    case "name":        quest.Name = reader.GetString(); break;
                    case "description": quest.Description = reader.GetString(); break;
                    case "startflag":
                    case "completionflag":
                        reader.Skip(); // computed from Id, ignored on read
                        break;
                    case "objectives":
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                if (reader.TokenType == JsonTokenType.StartObject)
                                    quest.Objectives.Add(ReadQuestObjective(ref reader));
                            }
                        }
                        break;
                    case "rewards":
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            // Old format: { "set_flags": [...], "set_variables": {...} }
                            quest.Rewards = MigrateOldRewards(ref reader);
                        }
                        else if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            // New format: [{ "type": "set_flag", ... }]
                            quest.Rewards = ReadActionList(ref reader);
                        }
                        else
                        {
                            reader.Skip();
                        }
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return quest;
        }

        private static QuestObjective ReadQuestObjective(ref Utf8JsonReader reader)
        {
            var obj = new QuestObjective();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) break;
                if (reader.TokenType != JsonTokenType.PropertyName) continue;

                string propName = reader.GetString();
                reader.Read();
                string norm = Normalize(propName);

                switch (norm)
                {
                    case "description": obj.Description = reader.GetString(); break;
                    case "type":        obj.Type = reader.GetString(); break;
                    case "flag":        obj.Flag = reader.GetString(); break;
                    case "variable":    obj.Variable = reader.GetString(); break;
                    case "value":       obj.Value = reader.GetInt32(); break;
                    default:            reader.Skip(); break;
                }
            }

            return obj;
        }

        private static List<DialogueAction> MigrateOldRewards(ref Utf8JsonReader reader)
        {
            var actions = new List<DialogueAction>();
            var setFlags = new List<string>();
            var setVariables = new Dictionary<string, string>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) break;
                if (reader.TokenType != JsonTokenType.PropertyName) continue;

                string propName = reader.GetString();
                reader.Read();
                string norm = Normalize(propName);

                switch (norm)
                {
                    case "setflags":
                        if (reader.TokenType == JsonTokenType.StartArray)
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                                setFlags.Add(reader.GetString());
                        break;
                    case "setvariables":
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                            {
                                if (reader.TokenType != JsonTokenType.PropertyName) continue;
                                string key = reader.GetString();
                                reader.Read();
                                setVariables[key] = reader.GetString();
                            }
                        }
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            foreach (var flag in setFlags)
                actions.Add(new DialogueAction { Type = "set_flag", Value = flag });
            foreach (var (key, value) in setVariables)
                actions.Add(new DialogueAction { Type = "set_variable", Key = key, Value = value });

            return actions;
        }

        private static List<DialogueAction> ReadActionList(ref Utf8JsonReader reader)
        {
            var actions = new List<DialogueAction>();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType != JsonTokenType.StartObject) continue;

                var action = new DialogueAction();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;

                    string propName = reader.GetString();
                    reader.Read();
                    string norm = Normalize(propName);

                    switch (norm)
                    {
                        case "type":  action.Type = reader.GetString(); break;
                        case "value": action.Value = reader.GetString(); break;
                        case "key":   action.Key = reader.GetString(); break;
                        case "color": action.Color = reader.GetString(); break;
                        case "text":  action.Text = reader.GetString(); break;
                        default:      reader.Skip(); break;
                    }
                }
                actions.Add(action);
            }

            return actions;
        }
    }
}
