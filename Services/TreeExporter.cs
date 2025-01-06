using System.IO;
using System.Text.Json;
using System.Windows;

namespace Decision_Trees_Visualizer.Services;
public class TreeExporter
{
    public bool SuppressMessages { get; set; } = false;
    public void ExportTreeToJson(List<Node> nodes, string filePath)
    {
        foreach(var node in nodes)
        {
            if (node.Label == "")
            {
                node.Label = node.TestInfo;
                break;
            }
            if (node.Label != null) break;
            if (node.GetType() != typeof(GraphvizNode)) break;
            node.Label = string.IsNullOrEmpty(((GraphvizNode)node).Test)
            ? $"gini = {((GraphvizNode)node).Gini}\nsamples = {((GraphvizNode)node).Samples}\nvalue = [{string.Join(", ", ((GraphvizNode)node).Value)}]\nclass = {((GraphvizNode)node).ClassName}"
            : $"{((GraphvizNode)node).Test}\ngini = {((GraphvizNode)node).Gini}\nsamples = {((GraphvizNode)node).Samples}\nvalue = [{string.Join(", ", ((GraphvizNode)node).Value)}]\nclass = {((GraphvizNode)node).ClassName}";

        }
        try
        {
            var json = JsonSerializer.Serialize(nodes, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(filePath, json);
            if (!SuppressMessages)
            {
                MessageBox.Show("Tree successfully exported to JSON.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            if(!SuppressMessages)
            {
                MessageBox.Show($"Error exporting tree: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                throw ex;
            }
        }
    }
}
