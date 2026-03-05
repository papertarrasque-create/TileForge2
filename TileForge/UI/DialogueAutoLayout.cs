using System.Collections.Generic;
using System.Linq;
using TileForge.Game;

namespace TileForge.UI;

public static class DialogueAutoLayout
{
    public const int DefaultColumnSpacing = 280;
    public const int DefaultRowSpacing = 160;

    public static void ApplyLayout(DialogueData dialogue, int columnSpacing = 280, int rowSpacing = 160)
    {
        if (dialogue == null || dialogue.Nodes == null || dialogue.Nodes.Count == 0)
            return;

        // Build node index by id
        var nodeById = new Dictionary<string, int>();
        for (int i = 0; i < dialogue.Nodes.Count; i++)
        {
            var node = dialogue.Nodes[i];
            if (!string.IsNullOrEmpty(node.Id) && !nodeById.ContainsKey(node.Id))
                nodeById[node.Id] = i;
        }

        // Build adjacency: for each node, collect target node indices
        var adjacency = new Dictionary<int, List<int>>();
        for (int i = 0; i < dialogue.Nodes.Count; i++)
        {
            var targets = new List<int>();
            var node = dialogue.Nodes[i];

            // NextNodeId target
            if (!string.IsNullOrEmpty(node.NextNodeId) && nodeById.TryGetValue(node.NextNodeId, out int nextIdx))
                targets.Add(nextIdx);

            // Choice targets
            if (node.Choices != null)
            {
                foreach (var choice in node.Choices)
                {
                    if (!string.IsNullOrEmpty(choice.NextNodeId) && nodeById.TryGetValue(choice.NextNodeId, out int choiceIdx))
                    {
                        if (!targets.Contains(choiceIdx))
                            targets.Add(choiceIdx);
                    }
                }
            }

            adjacency[i] = targets;
        }

        // BFS from root node (first node, or node with id "start" if exists)
        int rootIndex = 0;
        if (nodeById.TryGetValue("start", out int startIdx))
            rootIndex = startIdx;

        var visited = new HashSet<int>();
        var columns = new Dictionary<int, int>(); // nodeIndex -> column
        var queue = new Queue<int>();

        queue.Enqueue(rootIndex);
        visited.Add(rootIndex);
        columns[rootIndex] = 0;

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            int col = columns[current];

            if (adjacency.TryGetValue(current, out var targets))
            {
                foreach (int target in targets)
                {
                    if (!visited.Contains(target))
                    {
                        visited.Add(target);
                        columns[target] = col + 1;
                        queue.Enqueue(target);
                    }
                }
            }
        }

        // Place unreachable nodes in the last column + 1
        int maxCol = columns.Count > 0 ? columns.Values.Max() : 0;
        for (int i = 0; i < dialogue.Nodes.Count; i++)
        {
            if (!visited.Contains(i))
            {
                columns[i] = maxCol + 1;
            }
        }

        // Group nodes by column and assign row positions
        var columnGroups = new Dictionary<int, List<int>>();
        foreach (var kvp in columns)
        {
            if (!columnGroups.ContainsKey(kvp.Value))
                columnGroups[kvp.Value] = new List<int>();
            columnGroups[kvp.Value].Add(kvp.Key);
        }

        foreach (var kvp in columnGroups)
        {
            int col = kvp.Key;
            var nodesInCol = kvp.Value;
            for (int row = 0; row < nodesInCol.Count; row++)
            {
                int nodeIdx = nodesInCol[row];
                dialogue.Nodes[nodeIdx].EditorX = col * columnSpacing;
                dialogue.Nodes[nodeIdx].EditorY = row * rowSpacing;
            }
        }
    }
}
