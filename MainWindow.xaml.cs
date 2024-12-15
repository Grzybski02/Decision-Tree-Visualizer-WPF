using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Decision_Trees_Visualizer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ObservableCollection<Node> Nodes { get; set; }
    private GrapherSKL grapher;
    private GViewer gViewer;

    // Zmień na właściwość
    public List<string> ColorNames { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        grapher = new GrapherSKL();
        Nodes = new ObservableCollection<Node>();
        Nodes.CollectionChanged += Nodes_CollectionChanged;
        NodeGrid.ItemsSource = Nodes;

        ColorNames = new ColorList().GetPredefinedColorNames();
        var colorColumn = NodeGrid.Columns.OfType<DataGridComboBoxColumn>().FirstOrDefault(c => c.Header.ToString() == "Color");
        if (colorColumn != null)
        {
            colorColumn.ItemsSource = ColorNames;
        }
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedFormat = formatSelector.SelectedIndex;
        if (selectedFormat < 0)
        {
            System.Windows.MessageBox.Show("Please select a format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        string filePath = selectedFormat switch
        {
            0 => SelectFileWithExtension("Log files (*.log)|*.log"),
            1 => SelectFileWithExtension("Text files (*.txt)|*.txt"),
            2 => SelectFileWithExtension("JSON files (*.json)|*.json"),
            _ => null
        };

        if (filePath == null) return;

        List<Node> nodes;
        try
        {
            nodes = selectedFormat switch
            {
                0 => grapher.ParseTree(File.ReadAllLines(filePath), "Graphviz"),
                1 => grapher.ParseTree(File.ReadAllLines(filePath), "MLPDT"),
                2 => grapher.ParseTree(null, "JSON", filePath),
                _ => new List<Node>()
            };
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error loading tree: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (nodes.Count == 0)
        {
            System.Windows.MessageBox.Show("No nodes found in the selected file.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Nodes.Clear();
        foreach (var node in nodes)
        {
            if (string.IsNullOrEmpty(node.ColorName))
                node.ColorName = "White"; // Default color if missing
            Nodes.Add(node);
        }

        gViewer = grapher.RenderDecisionTree(Nodes.ToList());
        gViewer.MouseDoubleClick += GViewer_MouseDoubleClick;
        graphHost.Child = gViewer;
    }

    private string SelectFileWithExtension(string filter)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = filter;
        openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();

        return openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? openFileDialog.FileName : null;
    }

    private void NodeGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit)
        {
            UpdateGraphNode(e);
        }
    }

    private void UpdateGraphNode(DataGridCellEditEndingEventArgs e)
    {
        var editedNode = e.Row.Item as Node;
        if (editedNode == null) return;

        if (e.Column.Header.ToString() == "Color" && editedNode.IsClassLeaf)
        {
            var graphNode = gViewer.Graph.FindNode(editedNode.Id);
            if (graphNode != null)
            {
                graphNode.Attr.FillColor = new ColorList().GetColorByName(editedNode.ColorName);
            }
        }

        if (e.Column.Header.ToString() == "Label")
        {
            var graphNode = gViewer.Graph.FindNode(editedNode.Id);
            if (graphNode != null)
            {
                graphNode.LabelText = editedNode.Label;
            }
        }

        // Refresh the graph to reflect changes
        gViewer.Refresh();
    }

    private void GViewer_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
    {
        // Determine the object under the mouse cursor
        var objectUnderMouseCursor = gViewer.ObjectUnderMouseCursor;

        if (objectUnderMouseCursor != null && objectUnderMouseCursor.DrawingObject is Microsoft.Msagl.Drawing.Node graphNode)
        {
            // Get the node ID
            string nodeId = graphNode.Id;

            // Find the corresponding Node in your data
            var correspondingNode = Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (correspondingNode != null)
            {
                // Select the node in the DataGrid
                NodeGrid.SelectedItem = correspondingNode;
                NodeGrid.ScrollIntoView(correspondingNode);

                // Open the editing dialog
                EditNodeProperties(correspondingNode);
            }
        }
    }

    private void EditNodeProperties(Node node)
    {
        var editWindow = new EditNodeWindow(node, ColorNames);
        editWindow.Owner = this;
        var result = editWindow.ShowDialog();

        if (result == true)
        {
            // The PropertyChanged event should handle updates
            // Force refresh if necessary
            NodeGrid.Items.Refresh();
        }
    }

    private void Nodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (Node node in e.NewItems)
            {
                node.PropertyChanged += Node_PropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (Node node in e.OldItems)
            {
                node.PropertyChanged -= Node_PropertyChanged;
            }
        }
    }

    private void Node_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => Node_PropertyChanged(sender, e));
            return;
        }

        var editedNode = sender as Node;
        if (editedNode == null) return;

        if (gViewer == null || gViewer.Graph == null)
            return;

        var graphNode = gViewer.Graph.FindNode(editedNode.Id);
        if (graphNode != null)
        {
            if (e.PropertyName == nameof(Node.ColorName) && editedNode.IsClassLeaf)
            {
                graphNode.Attr.FillColor = new ColorList().GetColorByName(editedNode.ColorName);
            }

            if (e.PropertyName == nameof(Node.Label))
            {
                graphNode.LabelText = editedNode.Label;

                // Wymuszenie pełnego przeliczenia układu grafu
                gViewer.Graph = gViewer.Graph;
            }

            gViewer.Refresh();
        }
    }

    private void ExportJsonButton_Click(object sender, RoutedEventArgs e)
    {
        if (Nodes == null || Nodes.Count == 0)
        {
            System.Windows.MessageBox.Show("No tree loaded to export.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            DefaultExt = "json",
            AddExtension = true,
            InitialDirectory = Directory.GetCurrentDirectory()
        };

        if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            ExportTreeToJson(Nodes.ToList(), saveFileDialog.FileName);
        }
    }

    private void ExportTreeToJson(List<Node> nodes, string filePath)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(nodes, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(filePath, json);

            System.Windows.MessageBox.Show("Tree successfully exported to JSON.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error exporting tree: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }




}
