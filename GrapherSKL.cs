using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Decision_Trees_Visualizer;
internal class GrapherSKL
{
    public enum TreeFormat { MLPDT, Graphviz }
    private int nodeCounter = 0;
    private readonly string nodeIdPrefix = "Node";

    public Node ParseTree(string[] logLines, string format)
    {
        var root = new Node { Id = GetNextNodeId(), Label = "Root" };
        var nodeStack = new Stack<(Node node, int depth)>();
        nodeStack.Push((root, 0));
        if (format == "Graphviz")
        {
            foreach (var line in logLines)
            {
                int depth = CountDepth(line);
                var trimmedLine = line.TrimStart('|').Trim();

                if (trimmedLine.Contains("class:"))
                {
                    var classLabel = trimmedLine.Substring(trimmedLine.IndexOf("class:"));
                    var newNode = new Node { Id = GetNextNodeId(), Label = classLabel };
                    while (nodeStack.Count > 0 && nodeStack.Peek().depth >= depth) nodeStack.Pop();
                    if (nodeStack.Count > 0) nodeStack.Peek().node.Children.Add(newNode);
                    nodeStack.Push((newNode, depth));
                }
                else
                {
                    var condition = trimmedLine.Substring(trimmedLine.IndexOf("---") + 4);
                    var newNode = new Node { Id = GetNextNodeId(), Label = condition };
                    while (nodeStack.Count > 0 && nodeStack.Peek().depth >= depth) nodeStack.Pop();
                    if (nodeStack.Count > 0) nodeStack.Peek().node.Children.Add(newNode);
                    nodeStack.Push((newNode, depth));
                }
            }

            return root;
        }
        else
        {
            foreach (var line in logLines)
            {
                int depth = CountDepth(line)+1;
                var trimmedLine = line.Trim();

                if (trimmedLine.Contains(":"))
                {
                    var leafParts = trimmedLine.Split(':');
                    var label = leafParts[1].Trim();
                    var newNode = new Node { Id = GetNextNodeId(), Label = label };
                    while (nodeStack.Count > 0 && nodeStack.Peek().depth >= depth) nodeStack.Pop();
                    if (nodeStack.Count > 0) nodeStack.Peek().node.Children.Add(newNode);
                    nodeStack.Push((newNode, depth));
                }
                else
                {
                    var condition = trimmedLine.Replace('|', ' ').Trim();
                    var newNode = new Node { Id = GetNextNodeId(), Label = condition };
                    while (nodeStack.Count > 0 && nodeStack.Peek().depth >= depth) nodeStack.Pop();
                    if (nodeStack.Count > 0) nodeStack.Peek().node.Children.Add(newNode);
                    nodeStack.Push((newNode, depth));
                }
            }

            return root;
        }

    }

    private int CountDepth(string line)
    {
        return line.Count(t => t == '|');
    }


    private string GetNextNodeId()
    {
        return $"{nodeIdPrefix}{++nodeCounter}";
    }

    private void AddNodesToGraph(Graph graph, Node node)
    {
        foreach (var child in node.Children)
        {
            graph.AddEdge(node.Id, child.Id);
            graph.FindNode(node.Id).LabelText = node.Label;
            graph.FindNode(child.Id).LabelText = child.Label;
            AddNodesToGraph(graph, child);
        }
    }

    private void ColourGraph(Graph graph)
    {
        var colorPalette = new List<Color>
        {
            Color.Red,
            Color.Green,
            Color.Blue,
            Color.Orange,
            Color.Purple,
            Color.Yellow,
            Color.Magenta,
            Color.Cyan,
            Color.Brown,
            Color.Gray
        };

        var classesToColour = new Dictionary<string, Color>();
        int colorIndex = int.Parse(Random.Shared.NextInt64(10).ToString());

        foreach (var node in graph.Nodes)
        {
            string nodeLabel = node.LabelText;

            if (nodeLabel.StartsWith("class:"))
            {
                var className = nodeLabel.Substring(6).Trim();

                if (!classesToColour.ContainsKey(className))
                {
                    var assignedColor = colorPalette[colorIndex % colorPalette.Count];
                    classesToColour[className] = assignedColor;

                    colorIndex++;
                }

                node.Attr.FillColor = classesToColour[className];
            }

            else if (nodeLabel.Contains("("))
            {
                var className = nodeLabel.Substring(nodeLabel.IndexOf("("),nodeLabel.IndexOf(")")- nodeLabel.IndexOf("("));

                if (!classesToColour.ContainsKey(className))
                {
                    var assignedColor = colorPalette[colorIndex % colorPalette.Count];
                    classesToColour[className] = assignedColor;

                    colorIndex++;
                }

                node.Attr.FillColor = classesToColour[className];
            }
        }

    }

    public GViewer RenderDecisionTree(string filePath, TreeFormat format)
    {
        string[] treeLogLines = File.ReadAllLines(filePath);
        Graph graph = new Graph("Decision Tree");

        Node root = format switch
        {
            TreeFormat.Graphviz => ParseTree(treeLogLines, "Graphviz"),
            TreeFormat.MLPDT => ParseTree(treeLogLines, "MLPDT"),
            _ => throw new ArgumentException("Unsupported format.")
        };

        AddNodesToGraph(graph, root);
        ColourGraph(graph);

        GViewer gViewer = new GViewer { Graph = graph };
        // gviewer.ToolBarIsVisible = false;
        return gViewer;
    }


}