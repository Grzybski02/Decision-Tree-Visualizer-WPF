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
public partial class MainWindow : Window, INotifyPropertyChanged
{
    public ICommand LoadTreeCommand { get; }
    public ICommand ExportTreeCommand { get; }
    public ICommand ToggleNodeGridCommand { get; }
    public ICommand AboutCommand { get; }
    public event PropertyChangedEventHandler PropertyChanged;

    private ObservableCollection<Node> Nodes { get; set; }
    private GrapherSKL grapher;
    private GViewer gViewer;

    public List<string> ColorNames { get; set; }

    private const int MaxRecentFiles = 5;
    private List<(string FilePath, string Format)> RecentFiles = new List<(string, string)>();
    
    private bool IsNodeGridVisible = true;
    private Visibility _nodeGridVisibility = Visibility.Visible;
    private Visibility _nodeGridSplitterVisibility = Visibility.Visible;


    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        LoadTreeCommand = new RelayCommand(() => LoadMenuItem_Click(null, null));
        ExportTreeCommand = new RelayCommand(() => ExportJsonMenuItem_Click(null, null));
        ToggleNodeGridCommand = new RelayCommand(() => ToggleNodeGridMenuItem_Click(null, null));
        AboutCommand = new RelayCommand(() => AboutMenuItem_Click(null, null));


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


    private void LoadMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "All Supported Files (*.log;*.txt;*.json)|*.log;*.txt;*.json|Log files (*.log)|*.log|Text files (*.txt)|*.txt|JSON files (*.json)|*.json",
            InitialDirectory = Directory.GetCurrentDirectory()
        };

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        var formatDialog = new FormatSelectionDialog();
        if (formatDialog.ShowDialog() == true)
        {
            LoadFile(openFileDialog.FileName, formatDialog.SelectedFormat);
        }
    }



    private void ExportJsonMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ExportJsonButton_Click(sender, e); // Reuse existing logic
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void ToggleNodeGridMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (NodeGridVisibility == Visibility.Visible)
        {
            NodeGridVisibility = Visibility.Collapsed;
            NodeGridSplitterVisibility = Visibility.Collapsed;
        }
        else
        {
            NodeGridVisibility = Visibility.Visible;
            NodeGridSplitterVisibility = Visibility.Visible;
        }
    }


    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("Decision Trees Visualizer\nVersion 0.8 (15/12/2024)\nAuthor: Jacek Grzybowski", "About");
    }

    private string SelectFormatDialog()
    {
        var dialog = new System.Windows.MessageBoxResult();

        dialog = System.Windows.MessageBox.Show(
            "Please select the format of the file:\n\n" +
            "Yes - Graphviz (.log)\n" +
            "No - MLPDT (.txt)\n" +
            "Cancel - JSON (.json)",
            "Select Format",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        return dialog switch
        {
            System.Windows.MessageBoxResult.Yes => "Graphviz",
            System.Windows.MessageBoxResult.No => "MLPDT",
            System.Windows.MessageBoxResult.Cancel => "JSON",
            _ => null
        };
    }

    private void AddToRecentFiles(string filePath, string format)
    {
        if (RecentFiles.Any(f => f.FilePath == filePath))
            RecentFiles.RemoveAll(f => f.FilePath == filePath); // Usuń duplikaty

        RecentFiles.Insert(0, (filePath, format)); // Dodaj na początek listy

        if (RecentFiles.Count > MaxRecentFiles)
            RecentFiles.RemoveAt(MaxRecentFiles); // Ogranicz liczbę plików

        UpdateRecentFilesMenu();
    }


    private void UpdateRecentFilesMenu()
    {
        RecentFilesMenu.Items.Clear();

        if (RecentFiles.Count == 0)
        {
            RecentFilesMenu.Items.Add(new MenuItem { Header = "(None)", IsEnabled = false });
            return;
        }

        foreach (var (filePath, format) in RecentFiles)
        {
            var menuItem = new MenuItem
            {
                Header = System.IO.Path.GetFileName(filePath),
                ToolTip = $"{filePath} ({format})"
            };
            menuItem.Click += (sender, args) => LoadFile(filePath, format);
            RecentFilesMenu.Items.Add(menuItem);
        }
    }


    private void LoadFile(string filePath, string format)
    {
        try
        {
            var nodes = grapher.ParseTree(
                format == "JSON" ? null : File.ReadAllLines(filePath),
                format,
                format == "JSON" ? filePath : null
            );

            if (nodes == null || nodes.Count == 0)
            {
                System.Windows.MessageBox.Show("No nodes found in the selected file.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Nodes.Clear();
            foreach (var node in nodes)
            {
                if (string.IsNullOrEmpty(node.ColorName))
                    node.ColorName = "White"; // Domyślny kolor
                Nodes.Add(node);
            }

            gViewer = grapher.RenderDecisionTree(Nodes.ToList());
            graphHost.Child = gViewer;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error loading tree: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }



    private string DetectFormat(string filePath)
    {
        var extension = System.IO.Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".log" => "Graphviz",
            ".txt" => "MLPDT",
            ".json" => "JSON",
            _ => throw new NotSupportedException("Unsupported file format.")
        };
    }

    private string SelectFileWithExtension(string filter)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = filter;
        openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();

        return openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? openFileDialog.FileName : null;
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public Visibility NodeGridVisibility
    {
        get => _nodeGridVisibility;
        set
        {
            if (_nodeGridVisibility != value)
            {
                _nodeGridVisibility = value;
                OnPropertyChanged(nameof(NodeGridVisibility));
            }
        }
    }

    public Visibility NodeGridSplitterVisibility
    {
        get => _nodeGridSplitterVisibility;
        set
        {
            if (_nodeGridSplitterVisibility != value)
            {
                _nodeGridSplitterVisibility = value;
                OnPropertyChanged(nameof(NodeGridSplitterVisibility));
            }
        }
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
