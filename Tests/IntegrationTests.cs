using Decision_Trees_Visualizer.Services;
using Microsoft.Msagl.GraphViewerGdi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Integration;
using Xunit;
using Xunit.Sdk;

namespace Decision_Trees_Visualizer.Tests;
public class IntegrationTests
{
    [Fact]
    public void LoadAndExportJSONTree_ShouldWorkCorrectly()
    {
        // Arrange
        var fileService = new FileService();
        var exporter = new TreeExporter() { SuppressMessages = true };
        var testFilePath = "testTree.json";
        var exportFilePath = "exportedTree.json";

        var sampleJson = @"
        [
          {
            ""id"": ""Node14"",
            ""Label"": ""NodeTest"",
            ""ColorName"": ""White"",
            ""is_leaf"": false,
            ""depth"": 2,
            ""left_child"": 14,
            ""right_child"": 15,
            ""test_info"": ""\u003C= 1.00089""
          },
          {
            ""id"": ""Node15"",
            ""Label"": ""0 (A) (10944/12)"",
            ""ColorName"": ""YellowGreen"",
            ""is_leaf"": true,
            ""depth"": 3,
            ""left_child"": null,
            ""right_child"": null,
            ""test_info"": null
          },
          {
            ""id"": ""Node16"",
            ""Label"": ""1 (B) (11161/0)"",
            ""ColorName"": ""Tan"",
            ""is_leaf"": true,
            ""depth"": 3,
            ""left_child"": null,
            ""right_child"": null,
            ""test_info"": null
          }
        ]";

        File.WriteAllText(testFilePath, sampleJson);

        // Act
        var nodes = fileService.LoadFile(testFilePath, "JSON");
        exporter.ExportTreeToJson(nodes, exportFilePath);

        // Assert
        Assert.True(File.Exists(exportFilePath));
        var exportedContent = File.ReadAllText(exportFilePath);

        Assert.Contains("NodeTest", exportedContent);

        Assert.Contains("0 (A) (10944/12)", exportedContent);

        Assert.Contains("1 (B) (11161/0)", exportedContent);

        // Cleanup
        File.Delete(testFilePath);
        File.Delete(exportFilePath);
    }

    [Fact]
    public void LoadAndExportGraphvizTree_ShouldWorkCorrectly_WithGraphvizFormat()
    {
        // Arrange
        var fileService = new FileService();
        var exporter = new TreeExporter() { SuppressMessages = true };
        var testFilePath = "testGraphvizTree.log";
        var exportFilePath = "exportedGraphvizTree.json";

        var sampleGraphviz = @"digraph Tree {
node [shape=box, fontname=""helvetica""] ;
edge [fontname=""helvetica""] ;
0 [label=""petal length (cm) <= 2.45\ngini = 0.667\nsamples = 100.0%\nvalue = [0.333, 0.333, 0.333]\nclass = setosa""] ;
1 [label=""gini = 0.0\nsamples = 33.3%\nvalue = [1.0, 0.0, 0.0]\nclass = setosa""] ;
0 -> 1 [labeldistance=2.5, labelangle=45, headlabel=""True""] ;
}";

        File.WriteAllText(testFilePath, sampleGraphviz);

        // Act
        var nodes = fileService.LoadFile(testFilePath, "Graphviz");
        exporter.ExportTreeToJson(nodes.ToList(), exportFilePath);

        // Assert
        Assert.True(File.Exists(exportFilePath));
        var exportedContent = File.ReadAllText(exportFilePath);
        Assert.Contains("\"Label\": \"petal length (cm) \\u003C= 2.45\\n", exportedContent);

        // Cleanup
        File.Delete(testFilePath);
        File.Delete(exportFilePath);
    }

    [Fact]
    public void LoadAndExportMLPDTTree_ShouldWorkCorrectly()
    {
        // Arrange
        var fileService = new FileService();
        var exporter = new TreeExporter() { SuppressMessages = true };
        var testFilePath = "testMLPDTTree.txt";
        var exportFilePath = "exportedMLPDTTree.json";

        var sampleMLPDT = @"x11 <= -0.0097629
|  x9 <= -0.0182785
|  |  x8 <= -0.0430855 : 10 (c11) (5271/44)
|  |  x8 > -0.0430855
|  |  |  x10 <= -0.022732 : 1 (c2) (4965/921)
|  |  |  x10 > -0.022732 : 9 (c10) (4647/694)
";

        File.WriteAllText(testFilePath, sampleMLPDT);

        // Act
        var nodes = fileService.LoadFile(testFilePath, "MLPDT");
        exporter.ExportTreeToJson(nodes, exportFilePath);

        // Assert
        Assert.True(File.Exists(exportFilePath));
        var exportedContent = File.ReadAllText(exportFilePath);
        Assert.Contains("\"Label\": \"x11\"", exportedContent);
        Assert.Contains("\"Label\": \"x9\"", exportedContent);
        Assert.Contains("\"Label\": \"10 (c11) (5271/44)\"", exportedContent);

        // Cleanup
        File.Delete(testFilePath);
        File.Delete(exportFilePath);
    }

    [Fact]
    public void ExportTree_ShouldThrowException_WhenFilePathIsInvalid()
    {
        // Arrange
        var exporter = new TreeExporter() { SuppressMessages = true };
        var nodes = new List<Node>
    {
        new Node { Id = "1", Label = "Root", Depth = 0, IsClassLeaf = false }
    };
        var invalidFilePath = "Z:\\InvalidPath\\outputTree.json";

        // Act & Assert
        var exception = Assert.ThrowsAny<Exception>(() => exporter.ExportTreeToJson(nodes, invalidFilePath));
        Assert.Contains("Could not find a part of the path", exception.Message);
    }

}
