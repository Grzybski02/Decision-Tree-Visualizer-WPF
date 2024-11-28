using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System.Collections.ObjectModel;
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
        DataContext = this; // Ustawienie DataContext

        grapher = new GrapherSKL();
        Nodes = new ObservableCollection<Node>();
        NodeGrid.ItemsSource = Nodes;

        // Inicjalizacja ColorNames za pomocą publicznej metody
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
            // Defer the update until after the edit is committed
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                UpdateGraphNode(e);
            }));
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
}
