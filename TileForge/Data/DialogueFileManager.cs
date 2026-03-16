using System;
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
                {
                    MigrateV1ToV2(dialogue);
                    result.Add(dialogue);
                }
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
    /// Migrates v1 dialogue properties (RequiresFlag, SetsFlag, SetsVariable) to
    /// the v2 Conditions/Actions lists. Additive -- does not overwrite existing v2 data.
    /// </summary>
    public static void MigrateV1ToV2(DialogueData dialogue)
    {
        if (dialogue == null) return;

        if (dialogue.Nodes != null)
        {
            foreach (var node in dialogue.Nodes)
            {
                // RequiresFlag -> Conditions
                if (!string.IsNullOrEmpty(node.RequiresFlag) && (node.Conditions == null || node.Conditions.Count == 0))
                {
                    node.Conditions = new List<Condition>
                    {
                        new Condition { Type = "has_flag", Flag = node.RequiresFlag }
                    };
                    node.RequiresFlag = null;
                }

                // SetsFlag -> Actions
                if (!string.IsNullOrEmpty(node.SetsFlag))
                {
                    if (node.Actions == null || node.Actions.Count == 0)
                        node.Actions = new List<DialogueAction>();
                    node.Actions.Add(new DialogueAction { Type = "set_flag", Value = node.SetsFlag });
                    node.SetsFlag = null;
                }

                // SetsVariable -> Actions (format "key=value")
                if (!string.IsNullOrEmpty(node.SetsVariable))
                {
                    int eqIndex = node.SetsVariable.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        string key = node.SetsVariable.Substring(0, eqIndex);
                        string value = node.SetsVariable.Substring(eqIndex + 1);
                        if (node.Actions == null)
                            node.Actions = new List<DialogueAction>();
                        node.Actions.Add(new DialogueAction { Type = "set_variable", Key = key, Value = value });
                        node.SetsVariable = null;
                    }
                }

                // Migrate choices
                if (node.Choices != null)
                {
                    foreach (var choice in node.Choices)
                    {
                        if (!string.IsNullOrEmpty(choice.RequiresFlag) && (choice.Conditions == null || choice.Conditions.Count == 0))
                        {
                            choice.Conditions = new List<Condition>
                            {
                                new Condition { Type = "has_flag", Flag = choice.RequiresFlag }
                            };
                            choice.RequiresFlag = null;
                        }

                        if (!string.IsNullOrEmpty(choice.SetsFlag) && (choice.Actions == null || choice.Actions.Count == 0))
                        {
                            choice.Actions = new List<DialogueAction>
                            {
                                new DialogueAction { Type = "set_flag", Value = choice.SetsFlag }
                            };
                            choice.SetsFlag = null;
                        }
                    }
                }
            }
        }

        // Default route: if no Routes defined, create one pointing to the first node
        if (dialogue.Routes == null && dialogue.Nodes != null && dialogue.Nodes.Count > 0)
        {
            dialogue.Routes = new List<DialogueRoute>
            {
                new DialogueRoute { StartNode = dialogue.Nodes[0].Id }
            };
        }
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
