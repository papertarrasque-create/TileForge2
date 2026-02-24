using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TileForge.Data;
using TileForge.Game;

namespace TileForge.UI;

/// <summary>
/// Provides project-level data for browse-dropdown fields in editors.
/// </summary>
public interface IProjectContext
{
    /// <summary>Project directory path, or null if no project loaded.</summary>
    string ProjectDirectory { get; }

    /// <summary>Returns .tileforge file names (without extension) + "+ Create New..." as last item.</summary>
    string[] GetAvailableMaps();

    /// <summary>Returns dialogue JSON file names (without extension) from dialogues/ subdir + "+ Create New...".</summary>
    string[] GetAvailableDialogues();

    /// <summary>Returns all known flag names from quests and entity properties.</summary>
    string[] GetKnownFlags(List<QuestDefinition> quests, List<TileGroup> groups);

    /// <summary>Returns all known variable names from quests.</summary>
    string[] GetKnownVariables(List<QuestDefinition> quests, List<TileGroup> groups);
}

/// <summary>
/// Concrete implementation that reads from the filesystem.
/// </summary>
public class ProjectContext : IProjectContext
{
    public const string CreateNewItem = "+ Create New...";

    private readonly Func<string> _getProjectPath;

    public ProjectContext(Func<string> getProjectPath)
    {
        _getProjectPath = getProjectPath;
    }

    public string ProjectDirectory
    {
        get
        {
            var path = _getProjectPath();
            return path != null ? Path.GetDirectoryName(path) : null;
        }
    }

    public string[] GetAvailableMaps()
    {
        var dir = ProjectDirectory;
        if (dir == null || !Directory.Exists(dir))
            return new[] { CreateNewItem };

        var maps = Directory.GetFiles(dir, "*.tileforge")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        maps.Add(CreateNewItem);
        return maps.ToArray();
    }

    public string[] GetAvailableDialogues()
    {
        var dir = ProjectDirectory;
        if (dir == null) return new[] { CreateNewItem };

        string dialogueDir = Path.Combine(dir, "dialogues");
        if (!Directory.Exists(dialogueDir))
            return new[] { CreateNewItem };

        var dialogues = Directory.GetFiles(dialogueDir, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        dialogues.Add(CreateNewItem);
        return dialogues.ToArray();
    }

    public string[] GetKnownFlags(List<QuestDefinition> quests, List<TileGroup> groups)
    {
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (quests != null)
        {
            foreach (var quest in quests)
            {
                if (!string.IsNullOrWhiteSpace(quest.StartFlag))
                    flags.Add(quest.StartFlag);
                if (!string.IsNullOrWhiteSpace(quest.CompletionFlag))
                    flags.Add(quest.CompletionFlag);
                if (quest.Objectives != null)
                {
                    foreach (var obj in quest.Objectives)
                    {
                        if (obj.Type == "flag" && !string.IsNullOrWhiteSpace(obj.Flag))
                            flags.Add(obj.Flag);
                    }
                }
                if (quest.Rewards?.SetFlags != null)
                {
                    foreach (var f in quest.Rewards.SetFlags)
                    {
                        if (!string.IsNullOrWhiteSpace(f))
                            flags.Add(f);
                    }
                }
            }
        }

        if (groups != null)
        {
            foreach (var group in groups)
            {
                if (group.DefaultProperties == null) continue;
                foreach (var key in new[] { "on_kill_set_flag", "on_collect_set_flag" })
                {
                    if (group.DefaultProperties.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                        flags.Add(val);
                }
            }
        }

        return flags.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public string[] GetKnownVariables(List<QuestDefinition> quests, List<TileGroup> groups)
    {
        var vars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (quests != null)
        {
            foreach (var quest in quests)
            {
                if (quest.Objectives != null)
                {
                    foreach (var obj in quest.Objectives)
                    {
                        if ((obj.Type == "var>=" || obj.Type == "var==") && !string.IsNullOrWhiteSpace(obj.Variable))
                            vars.Add(obj.Variable);
                    }
                }
                if (quest.Rewards?.SetVariables != null)
                {
                    foreach (var key in quest.Rewards.SetVariables.Keys)
                    {
                        if (!string.IsNullOrWhiteSpace(key))
                            vars.Add(key);
                    }
                }
            }
        }

        if (groups != null)
        {
            foreach (var group in groups)
            {
                if (group.DefaultProperties == null) continue;
                foreach (var key in new[] { "on_kill_increment", "on_collect_increment" })
                {
                    if (group.DefaultProperties.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                        vars.Add(val);
                }
            }
        }

        return vars.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
