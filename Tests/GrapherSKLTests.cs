using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static System.Windows.Forms.DataFormats;

namespace Decision_Trees_Visualizer.Tests;
public class GrapherSKLTests
{
    [Fact]
    public void ParseMLPDTTree_ShouldReturnNodes_WhenValidMLPDTFormat()
    {
        // Arrange
        var grapher = new GrapherSKL();
        var logLines = new[]
        {
            "x11 <= -0.0097629",
            "|  x9 <= -0.0182785",
            "|  |  x8 <= -0.0430855 : 10 (c11) (5271/44)",
            "|  |  x8 > -0.0430855",
            "|  |  |  x10 <= -0.022732 : 1 (c2) (4965/921)",
            "|  |  |  x10 > -0.022732 : 9 (c10) (4647/694)",
            "|  x9 > -0.0182785"
        };
        var format = "MLPDT";

        // Act
        var nodes = grapher.ParseTree(logLines, format);

        // Assert
        Assert.NotNull(nodes);
        Assert.Equal(logLines.Length + 1, nodes.Count); // +1 for the root node

        Assert.Equal("x11", nodes[0].Label);
        Assert.Equal(false, nodes[0].IsClassLeaf);
        Assert.Equal(0, nodes[0].Depth); // even though the root node exists, it is not counted in the depth
        Assert.Equal("<= -0.0097629", nodes[0].TestInfo);

        Assert.Equal("x9", nodes[1].Label);
        Assert.Equal(false, nodes[1].IsClassLeaf);
        Assert.Equal(1, nodes[1].Depth);
        Assert.Equal("<= -0.0182785", nodes[1].TestInfo);

        Assert.Equal("x8", nodes[2].Label.Trim());
        Assert.Equal(false, nodes[2].IsClassLeaf);
        Assert.Equal(2, nodes[2].Depth);
        // last node before node inherits the test info from it's parent

        Assert.Equal("10 (c11) (5271/44)", nodes[3].Label);
        Assert.Equal(true, nodes[3].IsClassLeaf);
        Assert.Equal(3, nodes[3].Depth);
        Assert.Equal(null, nodes[3].TestInfo);
    }

    [Fact]
    public void ParseGraphvizTree_ShouldReturnNodes_WhenValidGraphvizFormat()
    {
        // Arrange
        var grapher = new GrapherSKL();
        var logLines = new[]
        {
        "digraph Tree {",
        "node [shape=box, fontname=\"helvetica\"] ;",
        "edge [fontname=\"helvetica\"] ;",
        "0 [label=\"petal length (cm) <= 2.45\\ngini = 0.667\\nsamples = 100.0%\\nvalue = [0.333, 0.333, 0.333]\\nclass = setosa\"] ;",
        "1 [label=\"gini = 0.0\\nsamples = 33.3%\\nvalue = [1.0, 0.0, 0.0]\\nclass = setosa\"] ;",
        "0 -> 1 [labeldistance=2.5, labelangle=45, headlabel=\"True\"] ;",
        "}"
    };
        var format = "Graphviz";

        // Act
        var nodes = grapher.ParseTree(logLines, format);

        // Assert
        Assert.NotNull(nodes);
        Assert.Equal(2, nodes.Count); // Graphviz doesn't need root
        Assert.Equal("petal length (cm) <= 2.45", ((GraphvizNode)nodes[0]).Test);
        Assert.False(nodes[0].IsClassLeaf);

        Assert.Null(((GraphvizNode)nodes[1]).Test);
        Assert.True(nodes[1].IsClassLeaf);
    }

    [Fact]
    public void ParseJSONTree_ShouldReturnNodes_WhenValidJsonFile()
    {
        // Arrange
        var grapher = new GrapherSKL();
        var jsonFilePath = "validTree.json";

        var sampleJson = @"
        [
            {
                ""id"": ""Node1"",
                ""Label"": ""Root"",
                ""ColorName"": ""White"",
                ""is_leaf"": false,
                ""depth"": 0,
                ""left_child"": 1,
                ""right_child"": 2,
                ""test_info"": ""<= 5.0""
            },
            {
                ""id"": ""Node2"",
                ""Label"": ""LeafA"",
                ""ColorName"": ""YellowGreen"",
                ""is_leaf"": true,
                ""depth"": 1,
                ""left_child"": null,
                ""right_child"": null,
                ""test_info"": null
            },
            {
                ""id"": ""Node3"",
                ""Label"": ""LeafB"",
                ""ColorName"": ""Tan"",
                ""is_leaf"": true,
                ""depth"": 1,
                ""left_child"": null,
                ""right_child"": null,
                ""test_info"": null
            }
        ]";

        File.WriteAllText(jsonFilePath, sampleJson);

        // Act
        var nodes = grapher.ParseTree(null, "JSON", jsonFilePath);

        // Assert
        Assert.NotNull(nodes);
        Assert.Equal(3, nodes.Count);

        Assert.Equal("Root", nodes[0].Label);
        Assert.False(nodes[0].IsClassLeaf);
        Assert.Equal(0, nodes[0].Depth);
        Assert.Equal("<= 5.0", nodes[0].TestInfo);

        Assert.Equal("LeafA", nodes[1].Label);
        Assert.True(nodes[1].IsClassLeaf);
        Assert.Equal(1, nodes[1].Depth);
        Assert.Null(nodes[1].TestInfo);

        Assert.Equal("LeafB", nodes[2].Label);
        Assert.True(nodes[2].IsClassLeaf);
        Assert.Equal(1, nodes[2].Depth);
        Assert.Null(nodes[2].TestInfo);

        // Cleanup
        File.Delete(jsonFilePath);
    }

    [Fact]
    public void ParseInvalidTree_ShouldThrowException_WhenUnsupportedFormat()
    {
        // Arrange
        var grapher = new GrapherSKL();
        var logLines = new[] { "Sample log line" };
        var unsupportedFormat = "UnsupportedFormat";

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => grapher.ParseTree(logLines, unsupportedFormat));
        Assert.Equal("Unsupported format: UnsupportedFormat", exception.Message);
    }


}
