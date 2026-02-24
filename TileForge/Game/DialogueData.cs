using System.Collections.Generic;

namespace TileForge.Game;

public class DialogueData
{
    public string Id { get; set; }
    public List<DialogueNode> Nodes { get; set; } = new();
}

public class DialogueNode
{
    public string Id { get; set; }
    public string Speaker { get; set; }
    public string Text { get; set; }
    public List<DialogueChoice> Choices { get; set; }  // null = auto-advance (linear)
    public string NextNodeId { get; set; }               // for linear sequences
    public string RequiresFlag { get; set; }             // conditional node
    public string SetsFlag { get; set; }                 // flag set when node is shown
    public string SetsVariable { get; set; }             // "key=value" format
}

public class DialogueChoice
{
    public string Text { get; set; }
    public string NextNodeId { get; set; }
    public string RequiresFlag { get; set; }  // hide choice if flag missing
    public string SetsFlag { get; set; }      // set flag when chosen
}
