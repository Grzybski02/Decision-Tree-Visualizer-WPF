using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using Microsoft.Win32;

namespace Decision_Trees_Visualizer.Services;
public class FileService
{
    public event Action<List<(string FilePath, string Format)>> RecentFilesUpdated;

    private const int MaxRecentFiles = 5;
    private List<(string FilePath, string Format)> RecentFiles = new List<(string, string)>();

    public string ShowOpenFileDialog()
    {
        var openFileDialog = new System.Windows.Forms.OpenFileDialog
        {
            Filter = "All Supported Files (*.log;*.txt;*.json)|*.log;*.txt;*.json|Log files (*.log)|*.log|Text files (*.txt)|*.txt|JSON files (*.json)|*.json",
            InitialDirectory = Directory.GetCurrentDirectory()
        };
        

        return openFileDialog.ShowDialog() == DialogResult.OK ? openFileDialog.FileName : null;
    }

    public List<(string FilePath, string Format)> GetRecentFiles()
    {
        return RecentFiles.ToList();
    }

    public string ShowSaveFileDialog(string filter)
    {
        var saveFileDialog = new System.Windows.Forms.SaveFileDialog
        {
            Filter = filter,
            DefaultExt = "json",
            AddExtension = true,
            InitialDirectory = Directory.GetCurrentDirectory()
        };

        return saveFileDialog.ShowDialog() == DialogResult.OK ? saveFileDialog.FileName : null;
    }

    public string SelectFormatDialog()
    {
        var formatDialog = new FormatSelectionDialog();
        if (formatDialog.ShowDialog() == true)
            return formatDialog.SelectedFormat;
        return null;
    }

    public List<Node> LoadFile(string filePath, string format)
    {
        // Poniższe założenie: GrapherSKL to klasa parsująca. Musisz dostosować do swojego kodu.
        var grapher = new GrapherSKL();

        try
        {
            var nodes = grapher.ParseTree(
                format == "JSON" ? null : File.ReadAllLines(filePath),
                format,
                format == "JSON" ? filePath : null
            );
            return nodes;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error loading tree: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

    public void AddToRecentFiles(string filePath, string format)
    {
        if (RecentFiles.Any(f => f.FilePath == filePath))
            RecentFiles.RemoveAll(f => f.FilePath == filePath);

        RecentFiles.Insert(0, (filePath, format));

        if (RecentFiles.Count > MaxRecentFiles)
            RecentFiles.RemoveAt(MaxRecentFiles);

        RecentFilesUpdated?.Invoke(GetRecentFiles());
    }

}
