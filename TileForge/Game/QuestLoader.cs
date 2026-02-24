using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return new List<QuestDefinition>();

        try
        {
            string json = File.ReadAllText(path);
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
                    case "startflag":   quest.StartFlag = reader.GetString(); break;
                    case "completionflag": quest.CompletionFlag = reader.GetString(); break;
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
                            quest.Rewards = ReadQuestRewards(ref reader);
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

        private static QuestRewards ReadQuestRewards(ref Utf8JsonReader reader)
        {
            var rewards = new QuestRewards();

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
                        {
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                                rewards.SetFlags.Add(reader.GetString());
                        }
                        break;
                    case "setvariables":
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                            {
                                if (reader.TokenType != JsonTokenType.PropertyName) continue;
                                string key = reader.GetString();
                                reader.Read();
                                rewards.SetVariables[key] = reader.GetString();
                            }
                        }
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return rewards;
        }
    }
}
