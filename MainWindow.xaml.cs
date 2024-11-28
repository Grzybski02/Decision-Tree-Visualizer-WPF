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
            _ => null
        };

        if (filePath == null) return;

        var nodes = grapher.ParseTree(File.ReadAllLines(filePath), selectedFormat == 0 ? "Graphviz" : "MLPDT");
        Nodes.Clear();
        foreach (var node in nodes)
        {
            node.ColorName = "White"; // Domyślny kolor dla wszystkich węzłów
            Nodes.Add(node);
        }

        gViewer = grapher.RenderDecisionTree(nodes);
        gViewer.MouseDoubleClick += GViewer_MouseDoubleClick; // Subscribe to the event
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


}
