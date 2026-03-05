using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TileForge.Game;

namespace TileForge.UI;

public class DialogueNodeWidget
{
    public string NodeId;
    public int NodeIndex;
    public Rectangle Bounds;          // World-space bounding box
    public Rectangle HeaderBounds;    // World-space header area
    public Rectangle InputPort;       // World-space left-center port
    public List<Rectangle> OutputPorts = new(); // World-space right-side ports
    public List<string> OutputTargets = new();  // Parallel: target node IDs (may be null)
    public List<string> OutputLabels = new();   // Parallel: choice text or "→" for NextNodeId

    public string SpeakerLabel;
    public string TextPreview;        // Truncated to MaxPreviewLength chars
    public bool IsSelected;

    public const int NodeWidth = 200;
    public const int HeaderHeight = 24;
    public const int PortSize = 10;
    public const int PortMargin = 4;
    public const int ChoiceRowHeight = 20;
    public const int BodyPadding = 6;
    public const int TextPreviewHeight = 20;
    public const int MinHeight = HeaderHeight + TextPreviewHeight + BodyPadding * 2;
    public const int MaxPreviewLength = 30;

    public static DialogueNodeWidget FromNode(DialogueNode node, int index)
    {
        var widget = new DialogueNodeWidget
        {
            NodeId = node.Id ?? $"node_{index}",
            NodeIndex = index,
            SpeakerLabel = node.Speaker ?? "(no speaker)",
            TextPreview = TruncateText(node.Text, MaxPreviewLength),
        };

        int x = node.EditorX ?? 0;
        int y = node.EditorY ?? 0;

        // Calculate height based on output ports
        bool hasChoices = node.Choices != null && node.Choices.Count > 0;
        int outputCount = hasChoices ? node.Choices.Count : 1;
        int outputSectionHeight = outputCount * ChoiceRowHeight;
        int totalHeight = Math.Max(MinHeight, HeaderHeight + TextPreviewHeight + outputSectionHeight + BodyPadding * 2);

        widget.Bounds = new Rectangle(x, y, NodeWidth, totalHeight);
        widget.HeaderBounds = new Rectangle(x, y, NodeWidth, HeaderHeight);

        // Input port: left center
        int inputPortY = y + totalHeight / 2 - PortSize / 2;
        widget.InputPort = new Rectangle(x - PortSize / 2, inputPortY, PortSize, PortSize);

        // Output ports: right side
        int outputStartY = y + HeaderHeight + TextPreviewHeight + BodyPadding;
        if (hasChoices)
        {
            for (int i = 0; i < node.Choices.Count; i++)
            {
                var choice = node.Choices[i];
                int portY = outputStartY + i * ChoiceRowHeight + ChoiceRowHeight / 2 - PortSize / 2;
                widget.OutputPorts.Add(new Rectangle(x + NodeWidth - PortSize / 2, portY, PortSize, PortSize));
                widget.OutputTargets.Add(choice.NextNodeId);
                widget.OutputLabels.Add(TruncateText(choice.Text, 20) ?? "...");
            }
        }
        else
        {
            // Single output for NextNodeId
            int portY = outputStartY + ChoiceRowHeight / 2 - PortSize / 2;
            widget.OutputPorts.Add(new Rectangle(x + NodeWidth - PortSize / 2, portY, PortSize, PortSize));
            widget.OutputTargets.Add(node.NextNodeId);
            widget.OutputLabels.Add("→");
        }

        return widget;
    }

    public int HitTestOutputPort(int worldX, int worldY)
    {
        for (int i = 0; i < OutputPorts.Count; i++)
        {
            var port = Inflate(OutputPorts[i], PortMargin);
            if (port.Contains(worldX, worldY))
                return i;
        }
        return -1;
    }

    public bool HitTestInputPort(int worldX, int worldY)
    {
        var port = Inflate(InputPort, PortMargin);
        return port.Contains(worldX, worldY);
    }

    public bool HitTestBody(int worldX, int worldY)
    {
        return Bounds.Contains(worldX, worldY);
    }

    public bool HitTestHeader(int worldX, int worldY)
    {
        return HeaderBounds.Contains(worldX, worldY);
    }

    public Vector2 GetOutputPortCenter(int portIndex)
    {
        if (portIndex < 0 || portIndex >= OutputPorts.Count)
            return Vector2.Zero;
        var port = OutputPorts[portIndex];
        return new Vector2(port.X + port.Width / 2f, port.Y + port.Height / 2f);
    }

    public Vector2 GetInputPortCenter()
    {
        return new Vector2(InputPort.X + InputPort.Width / 2f, InputPort.Y + InputPort.Height / 2f);
    }

    private static Rectangle Inflate(Rectangle rect, int amount)
    {
        return new Rectangle(rect.X - amount, rect.Y - amount,
                           rect.Width + amount * 2, rect.Height + amount * 2);
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return null;
        if (text.Length <= maxLength) return text;
        return text.Substring(0, maxLength - 3) + "...";
    }
}
