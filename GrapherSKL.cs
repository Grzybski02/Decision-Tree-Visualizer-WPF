using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System.IO;
using System.Text.RegularExpressions;

namespace Decision_Trees_Visualizer;
internal class GrapherSKL
{
    private int nodeCounter = 0;
    internal string selectedFormat;
    private readonly string nodeIdPrefix = "Node";
    private List<Color> colorPalette;

    public List<Node> ParseTree(string[] logLines, string format, string filePath = null)
    {
        selectedFormat = format;
        switch (format)
        {
            case "Graphviz":
                return ParseGraphvizTree(logLines);
            case "MLPDT":
                return ParseMLPDTTree(logLines);
            case "JSON":
                if (filePath == null)
                    throw new ArgumentException("File path is required for JSON format.");
                return ParseJsonTree(filePath);
            default:
                throw new NotSupportedException($"Unsupported format: {format}");
        }
    }

    private List<Node> ParseGraphvizTree(string[] logLines)
    {
        var nodes = new List<Node>();
        var edges = new List<(int Parent, int Child)>(); // Lista krawędzi

        foreach (var line in logLines)
        {
            AddGraphvizNode(nodes, edges, line);
        }

        // Uzupełnianie dzieci
        foreach (var (parent, child) in edges)
        {
            var parentNode = nodes.First(n => n.Id == parent.ToString());
            var childNode = nodes.First(n => n.Id == child.ToString());

            // Zamiana lewej i prawej strony
            if (parentNode.RightChildIndex == null)
                parentNode.RightChildIndex = nodes.IndexOf(childNode); // Teraz prawe dziecko
            else
                parentNode.LeftChildIndex = nodes.IndexOf(childNode); // Teraz lewe dziecko
        }


        return nodes;
    }

    // Pomocnicze metody
    private double ExtractDouble(string line) => double.Parse(Regex.Match(line, @"\d+\.\d+").Value.Replace('.', ','));
    private int ExtractInt(string line) => int.Parse(Regex.Match(line, @"\d+").Value);
    private List<int> ExtractValueList(string line)
    {
        return line
            .Split(new[] { '[', ']', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(x => x.Replace('.', ','))
            .Select(x => (int)double.Parse(x))
            .ToList();
    }






    private List<Node> ParseMLPDTTree(string[] logLines)
    {
        if (logLines == null || logLines.Length == 0)
            throw new ArgumentException("Input file is empty or null.", nameof(logLines));

        var nodes = new List<Node>();
        var levelIndexes = new Dictionary<int, int>();

        // Tworzenie korzenia
        var rootNode = new Node
        {
            Id = GetNextNodeId(),
            Label = "Root",
            Depth = 0 // Korzeń jest zawsze na poziomie 0
        };

        AddNodeToTree(nodes, levelIndexes, 0, rootNode, null); // Dodanie korzenia do drzewa

        foreach (var line in logLines)
        {
            int depth = CountDepth(line); // Liczba "|", która określa poziom węzła
            var trimmedLine = line.TrimStart('|').Trim();

            AddMLPDTNode(nodes, levelIndexes, depth + 1, trimmedLine); // Przesunięcie o 1, bo korzeń to poziom 0
        }

        return nodes;
    }

    public List<Node> ParseJsonTree(string filePath)
    {
        try
        {
            var jsonContent = File.ReadAllText(filePath);
            var nodes = System.Text.Json.JsonSerializer.Deserialize<List<Node>>(jsonContent);

            if (nodes == null || nodes.Count == 0)
            {
                throw new InvalidOperationException("The JSON file is empty or has an invalid structure.");
            }

            return nodes;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing JSON tree: {ex.Message}", ex);
        }
    }

    private void AddGraphvizNode(List<Node> nodes, List<(int, int)> edges, string line)
    {
        // Parsowanie węzłów
        if (line.Contains("[label="))
        {
            var match = Regex.Match(line, @"(\d+) \[label=""(.+?)""\]");
            if (match.Success)
            {
                var nodeId = int.Parse(match.Groups[1].Value);
                var label = match.Groups[2].Value;

                var node = new GraphvizNode { Id = nodeId.ToString() };

                // Podział etykiety na linie (uwzględnia liście z 4 liniami)
                var lines = label.Split(new[] { "\\n" }, StringSplitOptions.None);

                if (lines.Length > 4) // Węzeł wewnętrzny
                {
                    node.Test = lines[0];
                    node.Gini = ExtractDouble(lines[1]);
                    node.Samples = ExtractInt(lines[2]);
                    node.Value = ExtractValueList(lines[3]);
                    node.ClassName = lines[4].Split('=')[1].Trim();
                }
                else // Liść
                {
                    node.Gini = ExtractDouble(lines[0]);
                    node.Samples = ExtractInt(lines[1]);
                    node.Value = ExtractValueList(lines[2]);
                    node.ClassName = lines[3].Split('=')[1].Trim();
                    node.IsClassLeaf = true;
                }

                nodes.Add(node);
            }
        }
        // Parsowanie krawędzi
        else if (line.Contains("->"))
        {
            var edgeMatch = Regex.Match(line, @"(\d+) -> (\d+)");
            if (edgeMatch.Success)
            {
                var parent = int.Parse(edgeMatch.Groups[1].Value);
                var child = int.Parse(edgeMatch.Groups[2].Value);
                edges.Add((parent, child));
            }
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
                parentNode.TestInfo = condition;
            }
            else if (parentNode.RightChildIndex == null)
            {
                parentNode.RightChildIndex = currentIndex;
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
        var graphNode = graph.AddNode(currentNode.Id);

        // Ustawianie etykiety węzła z informacją o nazwie i etykiecie lewej krawędzi
        graphNode.LabelText = string.IsNullOrWhiteSpace(currentNode.TestInfo)
            ? currentNode.Label
            : $"{currentNode.Label}\n{currentNode.TestInfo}";


        // Dzieci
        if (currentNode.LeftChildIndex.HasValue)
        {
            graph.AddEdge(currentNode.Id, nodes[currentNode.LeftChildIndex.Value].Id).LabelText = ""; // Bez etykiety
            AddNodesToGraph(graph, nodes, currentNode.LeftChildIndex.Value);
        }

        if (currentNode.RightChildIndex.HasValue)
        {
            graph.AddEdge(currentNode.Id, nodes[currentNode.RightChildIndex.Value].Id).LabelText = ""; // Bez etykiety
            AddNodesToGraph(graph, nodes, currentNode.RightChildIndex.Value);
        }
    }

    private void AddGraphvizNodesToGraph(Graph graph, List<Node> nodes, int currentIndex)
    {
        if (currentIndex < 0 || currentIndex >= nodes.Count) return;

        GraphvizNode currentNode = (GraphvizNode)nodes[currentIndex];
        var graphNode = graph.AddNode(currentNode.Id);

        // Formatowanie informacji w węźle
        graphNode.LabelText = string.IsNullOrEmpty(currentNode.Test)
            ? $"gini = {currentNode.Gini}\nsamples = {currentNode.Samples}\nvalue = [{string.Join(", ", currentNode.Value)}]\nclass = {currentNode.ClassName}"
            : $"{currentNode.Test}\ngini = {currentNode.Gini}\nsamples = {currentNode.Samples}\nvalue = [{string.Join(", ", currentNode.Value)}]\nclass = {currentNode.ClassName}";

        currentNode.Label = graphNode.LabelText;

        // Dodanie dzieci
        if (currentNode.LeftChildIndex.HasValue)
        {
            graph.AddEdge(currentNode.Id, nodes[currentNode.LeftChildIndex.Value].Id).LabelText = "";
            AddGraphvizNodesToGraph(graph, nodes, currentNode.LeftChildIndex.Value);
        }

        if (currentNode.RightChildIndex.HasValue)
        {
            graph.AddEdge(currentNode.Id, nodes[currentNode.RightChildIndex.Value].Id).LabelText = "";
            AddGraphvizNodesToGraph(graph, nodes, currentNode.RightChildIndex.Value);
        }
    }



    public void ColourGraph(Graph graph, List<Node> nodes)
    {
        ColorList colorList = new ColorList();
        var classesToColour = new Dictionary<string, (Color color, string colorName)>();
        foreach (var node in graph.Nodes)
        {
            string nodeLabel = node.LabelText;
            string[] lines = nodeLabel.Split("\n");
            string className = null;

            // Sprawdzenie, czy węzeł jest liściem klasy
            switch (selectedFormat)
            {
                case "Graphviz":
                    if (lines.Count() == 4)
                    {
                        className = lines[3];
                    }
                    break;

                default:
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
                    break;
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
                node.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Ellipse;

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
                node.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Box; // Możesz zmienić na inny kształt, jeśli chcesz
            }
        }
    }

    public GViewer RenderDecisionTree(List<Node> Nodes)
    {
        Graph graph = new Graph("Decision Tree");

        if (selectedFormat == "Graphviz")
            AddGraphvizNodesToGraph(graph, Nodes, 0); // Startujemy od korzenia na indeksie 0
        else
            AddNodesToGraph(graph, Nodes, 0); // Startujemy od korzenia na indeksie 0
        ColourGraph(graph, Nodes);

        return new GViewer { Graph = graph };
    }
}