using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Decision_Trees_Visualizer.Services;
public class TreeExporter
{
    public void ExportTreeToJson(List<Node> nodes, string filePath)
    {
        try
        {
            var json = JsonSerializer.Serialize(nodes, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(filePath, json);
            MessageBox.Show("Tree successfully exported to JSON.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting tree: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
