using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using static System.Net.Mime.MediaTypeNames;

namespace Decision_Trees_Visualizer;
internal class GrapherSKL
{
    public enum TreeFormat { Graphviz, MLPDT }
    private int nodeCounter = 0;
    private readonly string nodeIdPrefix = "Node";
    private List<Color> colorPalette;

    public List<Node> ParseTree(string[] logLines, string format)
    {
        var nodes = new List<Node>(); // Globalna lista węzłów
        var levelIndexes = new Dictionary<int, int>(); // Mapowanie poziomów drzewa na indeksy w liście


        var newNode = new Node
        {
            Id = GetNextNodeId(),
            Label = "Root"
        };

        AddNodeToTree(nodes, levelIndexes, 0, newNode, "test");

        foreach (var line in logLines)
        {
            int depth = CountDepth(line);
            var trimmedLine = line.TrimStart('|').Trim();

            if (format == "Graphviz")
            {
                AddGraphvizNode(nodes, levelIndexes, depth, trimmedLine);
            }
            else
            {
                AddMLPDTNode(nodes, levelIndexes, depth + 1, trimmedLine);
            }
        }

        return nodes; // Zwracamy całą listę węzłów
    }

    private void AddGraphvizNode(List<Node> nodes, Dictionary<int, int> levelIndexes, int depth, string trimmedLine)
    {
        if (trimmedLine.Contains("class:"))
        {
            // Węzeł liścia
            var classLabel = trimmedLine.Substring(trimmedLine.IndexOf("class:")).Trim();
            var leafNode = new Node { Id = GetNextNodeId(), Label = classLabel, IsClassLeaf=true };

            AddNodeToTree(nodes, levelIndexes, depth, leafNode, null);
        }
        else
        {
            // Węzeł cechy
            var featureWithTest = trimmedLine.Split(new[] { "---" }, StringSplitOptions.None);
            var featureName = featureWithTest[0].Trim().Split(' ')[0]; // Wyodrębnij nazwę cechy
            var test = featureWithTest[1].Trim();

            var newNode = new Node { Id = GetNextNodeId(), Label = featureName };

            AddNodeToTree(nodes, levelIndexes, depth, newNode, test);
        }
    }

    private void AddMLPDTNode(List<Node> nodes, Dictionary<int, int> levelIndexes, int depth, string trimmedLine)
    {
        if (trimmedLine.Contains(":"))
        {
            // Węzeł liścia
            var leafParts = trimmedLine.Split(':');
            var label = leafParts[1].Trim();
            var test = ExtractCondition(leafParts[0].Replace('|', ' ').Trim());
            var leafNode = new Node { Id = GetNextNodeId(), Label = label, IsClassLeaf = true };

            AddNodeToTree(nodes, levelIndexes, depth, leafNode, test);
            MoveLabel(leafParts[0].Replace('|', ' ').Trim(), nodes);
        }
        else
        {
            // Węzeł cechy
            var condition = trimmedLine.Replace('|', ' ').Trim(); // Usunięcie prefiksu "|"
            var featureName = ExtractFeatureName(condition);      // Wyciągamy tylko nazwę cechy
            var test = ExtractCondition(condition);              // Wyciągamy warunek testowy

            var newNode = new Node
            {
                Id = GetNextNodeId(),
                Label = featureName
            };

            AddNodeToTree(nodes, levelIndexes, depth, newNode, test);
            MoveLabel(featureName, nodes);
        }
    }

    private void MoveLabel(string label, List<Node> nodes)
    {
        string previousNodeLabel = nodes[nodes.Count - 2].Label;
        if (!previousNodeLabel.Contains('('))
        {
            label = label.Split('<')[0];
            label = label.Split('>')[0];
            nodes[nodes.Count - 2].Label = label;
        }
        else
        {
            if (nodes[nodes.Count - 3].Label.Contains('(')) return;
            label = label.Split('<')[0];
            label = label.Split('>')[0];
            nodes[nodes.Count - 3].Label = label;
        }
    }
    private string ExtractFeatureName(string condition)
    {
        // Wyodrębnij nazwę cechy (np. "F1" z "F1 <= -0.104241")
        int index = condition.IndexOf(" ");
        return index == -1 ? condition : condition.Substring(0, index);
    }

    private string ExtractCondition(string condition)
    {
        // Wyodrębnij warunek (np. "<= -0.104241" z "F1 <= -0.104241")
        int index = condition.IndexOf(" ");
        return index == -1 ? null : condition.Substring(index + 1).Trim();
    }

    private void AddNodeToTree(List<Node> nodes, Dictionary<int, int> levelIndexes, int depth, Node newNode, string condition)
    {
        newNode.Depth = depth; // Ustaw głębokość węzła
        nodes.Add(newNode);
        int currentIndex = nodes.Count - 1;

        if (levelIndexes.ContainsKey(depth - 1))
        {
            // Znajdź rodzica i przypisz dzieci
            int parentIndex = levelIndexes[depth - 1];
            var parentNode = nodes[parentIndex];

            if (parentNode.LeftChildIndex == null)
            {
                parentNode.LeftChildIndex = currentIndex;
                parentNode.LeftEdgeLabel = condition;
            }
            else if (parentNode.RightChildIndex == null)
            {
                parentNode.RightChildIndex = currentIndex;
                parentNode.RightEdgeLabel = condition;
            }
            else
            {
                throw new InvalidOperationException("Węzeł nadrzędny ma już dwóch potomków.");
            }
        }

        // Zaktualizuj bieżący poziom węzła
        levelIndexes[depth] = currentIndex;
    }



    private int CountDepth(string line)
    {
        return line.Count(t => t == '|');
    }


    private string GetNextNodeId()
    {
        return $"{nodeIdPrefix}{++nodeCounter}";
    }

    private void AddNodesToGraph(Graph graph, List<Node> nodes, int currentIndex)
    {
        if (currentIndex < 0 || currentIndex >= nodes.Count) return;

        var currentNode = nodes[currentIndex];
        graph.AddNode(currentNode.Id).LabelText = currentNode.Label;

        if (currentNode.LeftChildIndex.HasValue)
        {
            var leftEdge = graph.AddEdge(currentNode.Id, nodes[currentNode.RightChildIndex.Value].Id);
            leftEdge.LabelText = currentNode.RightEdgeLabel;
            AddNodesToGraph(graph, nodes, currentNode.RightChildIndex.Value);
        }

        if (currentNode.RightChildIndex.HasValue)
        {
            var rightEdge = graph.AddEdge(currentNode.Id, nodes[currentNode.LeftChildIndex.Value].Id);
            rightEdge.LabelText = currentNode.LeftEdgeLabel;
            AddNodesToGraph(graph, nodes, currentNode.LeftChildIndex.Value);
        }
    }




    public void ColourGraph(Graph graph, List<Node> nodes)
    {
        ColorList colorList = new ColorList();
        var classesToColour = new Dictionary<string, (Color color, string colorName)>();

        foreach (var node in graph.Nodes)
        {
            string nodeLabel = node.LabelText;
            string className = null;

            // Sprawdzenie, czy węzeł jest liściem klasy
            if (nodeLabel.StartsWith("class:"))
            {
                className = nodeLabel.Substring(6).Trim();
            }
            else if (nodeLabel.Contains("("))
            {
                int startIndex = nodeLabel.IndexOf("(") + 1;
                int length = nodeLabel.IndexOf(")") - startIndex;
                className = nodeLabel.Substring(startIndex, length);
            }

            if (className != null)
            {
                // Przypisanie koloru dla klasy
                if (!classesToColour.ContainsKey(className))
                {
                    var (assignedColor, colorLabel) = colorList.GetColorForClass(className);
                    classesToColour[className] = (assignedColor, colorLabel);
                }

                // Ustawienie koloru w grafie
                var (color, colorName) = classesToColour[className];
                node.Attr.FillColor = color;

                // Ustawienie kształtu na elipsę
                node.Attr.Shape = Shape.Ellipse;

                // Aktualizacja tabeli
                var correspondingNode = nodes.FirstOrDefault(n => n.Id == node.Id);
                if (correspondingNode != null)
                {
                    correspondingNode.ColorName = colorName;
                }
            }
            else
            {
                // Opcjonalnie, ustaw kształt dla innych węzłów
                node.Attr.Shape = Shape.Box; // Możesz zmienić na inny kształt, jeśli chcesz
            }
        }
    }









    public GViewer RenderDecisionTree(List<Node> Nodes)
    {
        Graph graph = new Graph("Decision Tree");


        AddNodesToGraph(graph, Nodes, 0); // Startujemy od korzenia na indeksie 0
        ColourGraph(graph,Nodes);

        // gviewer.ToolBarIsVisible = false;
        return new GViewer { Graph = graph };
    }





}