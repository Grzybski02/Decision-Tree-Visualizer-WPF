using Decision_Trees_Visualizer.Services;
using Microsoft.Msagl.GraphViewerGdi;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Decision_Trees_Visualizer;
public partial class MainWindow : Window, INotifyPropertyChanged
{
    public ICommand LoadTreeCommand { get; }
    public ICommand ExportTreeCommand { get; }
    public ICommand ToggleNodeGridCommand { get; }
    public ICommand AboutCommand { get; }

    private ObservableCollection<Node> Nodes { get; set; }

    // Zarządzanie plikami i historią plików
    private FileService fileService;
    private NodeGraphManager nodeGraphManager;
    private TreeExporter treeExporter;

    public List<string> ColorNames { get; set; }
    private GViewer gViewer;

    public ObservableCollection<MenuItem> RecentFilesCollection { get; set; } = new ObservableCollection<MenuItem>();
    private bool _isRecentFilesAvailable;
    public bool IsRecentFilesAvailable
    {
        get => _isRecentFilesAvailable;
        set
        {
            if (_isRecentFilesAvailable != value)
            {
                _isRecentFilesAvailable = value;
                OnPropertyChanged(nameof(IsRecentFilesAvailable));
            }
        }
    }

    private bool IsNodeGridVisible = true;
    private Visibility _nodeGridVisibility = Visibility.Visible;
    private Visibility _nodeGridSplitterVisibility = Visibility.Visible;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        // Inicjalizacja usług
        fileService = new FileService();
        treeExporter = new TreeExporter();
        nodeGraphManager = new NodeGraphManager();

        // Przypięcie eventów z serwisów do UI (np. aktualizacja menu ostatnich plików)
        fileService.RecentFilesUpdated += UpdateRecentFilesMenu;

        // Inicjalizacja komend
        LoadTreeCommand = new RelayCommand(() => LoadTree());
        ExportTreeCommand = new RelayCommand(() => ExportToJson());
        ToggleNodeGridCommand = new RelayCommand(() => ToggleNodeGridVisibility());
        AboutCommand = new RelayCommand(() => ShowAbout());

        gViewer = new GViewer()
        {
            ToolBarIsVisible = false
        };

        gViewer.MouseDown += GViewer_MouseDown;
        Nodes = new ObservableCollection<Node>();
        Nodes.CollectionChanged += nodeGraphManager.Nodes_CollectionChanged;
        nodeGraphManager.Initialize(Nodes, graphHost, gViewer); // Przekazujemy referencje do zarządzania grafem
        graphHost.Child = gViewer;
        ToggleNodeGridVisibility();
        NodeGrid.ItemsSource = Nodes;

        ColorNames = new ColorList().GetPredefinedColorNames();
        var colorColumn = NodeGrid.Columns.OfType<DataGridComboBoxColumn>().FirstOrDefault(c => c.Header.ToString() == "Color");
        if (colorColumn != null)
        {
            colorColumn.ItemsSource = ColorNames;
        }

        // Przypięcie eventów do zdarzeń DataGrid
        NodeGrid.CellEditEnding += nodeGraphManager.NodeGrid_CellEditEnding;
    }

    private void LoadMenuItem_Click(object sender, RoutedEventArgs e)
    {
        LoadTree();
    }

    private void ExportJSON_Click(object sender, RoutedEventArgs e)
    {
        ExportToJson();
    }

    private void ExportOther_Click(object sender, RoutedEventArgs e)
    {
        if (gViewer.Graph == null)
        {
            System.Windows.MessageBox.Show("No graph to export.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        gViewer.SaveButtonPressed();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ShowAbout();
    }

    private void ToggleNodeGridMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ToggleNodeGridVisibility();
    }

    private void LoadTree()
    {
        var result = fileService.ShowOpenFileDialog();
        if (result == null) return;

        string format = Path.GetExtension(result).Substring(1).ToUpper();
        switch (format)
        {
            case "LOG":
                format = "Graphviz";
                break;
            case "TXT":
                format = "MLPDT";
                break;
            case "JSON":
                break;
            default:
                System.Windows.MessageBox.Show("Unsupported file format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
        }

        var nodes = fileService.LoadFile(result, format);
        if (nodes == null || nodes.Count == 0)
        {
            System.Windows.MessageBox.Show("No nodes found in the selected file.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Zaktualizuj kolekcję Nodes
        Nodes.Clear();
        foreach (var n in nodes)
        {
            if (string.IsNullOrEmpty(n.ColorName))
                n.ColorName = "White";
            Nodes.Add(n);
        }

        nodeGraphManager.RenderGraph(Nodes.ToList(), format);

        // Dodaj do ostatnich plików
        fileService.AddToRecentFiles(result, format);
        Toolbar.Visibility = Visibility.Visible;
    }

    private void ExportToJson()
    {
        if (Nodes == null || Nodes.Count == 0)
        {
            System.Windows.MessageBox.Show("No tree loaded to export.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var savePath = fileService.ShowSaveFileDialog("JSON files (*.json)|*.json");
        if (savePath == null) return;

        treeExporter.ExportTreeToJson(Nodes.ToList(), savePath);
    }

    private void ShowAbout()
    {
        System.Windows.MessageBox.Show("Decision Trees Visualizer\nVersion 0.8 (15/12/2024)\nAuthor: Jacek Grzybowski", "About");
    }

    private void ToggleNodeGridVisibility()
    {
        if (NodeGridVisibility == Visibility.Visible)
        {
            NodeGridVisibility = Visibility.Collapsed;
            NodeGridSplitterVisibility = Visibility.Collapsed;

            // Rozciągnij obszar wykresu
            var grid = (Grid)graphHost.Parent;
            grid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            grid.ColumnDefinitions[2].Width = new GridLength(0);
        }
        else
        {
            NodeGridVisibility = Visibility.Visible;
            NodeGridSplitterVisibility = Visibility.Visible;

            // Przywróć rozmiary
            var grid = (Grid)graphHost.Parent;
            grid.ColumnDefinitions[0].Width = new GridLength(4, GridUnitType.Star);
            grid.ColumnDefinitions[2].Width = new GridLength(2, GridUnitType.Star);
        }
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

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void UpdateRecentFilesMenu(List<(string FilePath, string Format)> recentFiles)
    {
        RecentFilesCollection.Clear();

        if (recentFiles == null || recentFiles.Count == 0)
        {
            RecentFilesCollection.Add(new MenuItem { Header = "(None)", IsEnabled = false });
            IsRecentFilesAvailable = false;
        }
        else
        {
            foreach (var (filePath, format) in recentFiles)
            {
                var menuItem = new MenuItem
                {
                    Header = System.IO.Path.GetFileName(filePath),
                    ToolTip = $"{filePath} ({format})",
                    Command = new RelayCommand(() =>
                    {
                        var nodes = fileService.LoadFile(filePath, format);
                        if (nodes == null || nodes.Count == 0)
                        {
                            MessageBox.Show("No nodes found in the selected file.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        Nodes.Clear();
                        foreach (var n in nodes)
                        {
                            if (string.IsNullOrEmpty(n.ColorName))
                                n.ColorName = "White";
                            Nodes.Add(n);
                        }

                        nodeGraphManager.RenderGraph(Nodes.ToList(), format);
                    })
                };

                RecentFilesCollection.Add(menuItem);
            }
            IsRecentFilesAvailable = true;
        }
    }

    private bool _isPanMode = false;
    public bool IsPanMode
    {
        get => _isPanMode;
        set
        {
            if (_isPanMode != value)
            {
                _isPanMode = value;
                OnPropertyChanged(nameof(IsPanMode));
            }
        }
    }

    private bool _isRectangleZoomMode = false;
    public bool IsRectangleZoomMode
    {
        get => _isRectangleZoomMode;
        set
        {
            if (_isRectangleZoomMode != value)
            {
                _isRectangleZoomMode = value;
                OnPropertyChanged(nameof(IsRectangleZoomMode));
            }
        }
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        gViewer.ZoomInPressed();
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        gViewer.ZoomOutPressed();
    }

    private void FitToScreen_Click(object sender, RoutedEventArgs e)
    {
        gViewer.WindowZoomButtonPressed = false;
        gViewer.PanButtonPressed = false;
        gViewer.Transform = null;
        gViewer.Refresh();

        // Resetuj stany ToggleButton
        IsPanMode = false;
        IsRectangleZoomMode = false;
    }

    private void Pan_Click(object sender, RoutedEventArgs e)
    {
        if (!IsPanMode)
        {
            gViewer.PanButtonPressed = false;
            PanIcon.IsChecked = false;

            IsPanMode = false;
        }
        else
        {
            if (gViewer.Graph == null) return;
            gViewer.PanButtonPressed = true;
            PanIcon.IsChecked = true;

            gViewer.WindowZoomButtonPressed = false;
            RectangleIcon.IsChecked = false;

            IsPanMode = true;
        }
    }

    private void Rectangle_Click(object sender, RoutedEventArgs e)
    {
        if (!IsRectangleZoomMode)
        {
            gViewer.WindowZoomButtonPressed = false;
            RectangleIcon.IsChecked = false;

            IsRectangleZoomMode = false;
        }
        else
        {
            if (gViewer.Graph == null) return;
            gViewer.WindowZoomButtonPressed = true;
            RectangleIcon.IsChecked = true;

            gViewer.PanButtonPressed = false;
            PanIcon.IsChecked = false;

            IsRectangleZoomMode = true;
        }
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        gViewer.Undo();
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        gViewer.Redo();
    }

    private Node selectedNode;
    public Node SelectedNode
    {
        get => selectedNode;
        set
        {
            if (selectedNode != value)
            {
                selectedNode = value;
                OnPropertyChanged(nameof(SelectedNode));
                OnSelectedNodeChanged();
            }
        }
    }


    private void GViewer_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
    {
        // Sprawdź, który obiekt został kliknięty
        var clickedObject = gViewer.GetObjectAt(e.Location);

        if (clickedObject is Microsoft.Msagl.GraphViewerGdi.DNode graphNode)
        {
            // Znajdź odpowiadający węzeł w kolekcji Nodes
            SelectedNode = Nodes.FirstOrDefault(n => n.Id == graphNode.Node.Id);

            if (SelectedNode != null)
            {
                // Automatycznie przewiń NodeGrid do zaznaczonego wiersza
                NodeGrid.ScrollIntoView(SelectedNode);
            }
        }
    }

    private void OnSelectedNodeChanged()
    {
        if (gViewer.Graph != null)
        {
            // Resetowanie stylów wszystkich węzłów
            foreach (var node in gViewer.Graph.Nodes)
            {
                node.Attr.Color = Microsoft.Msagl.Drawing.Color.Black;
                node.Attr.LineWidth = 1;
            }

            // Podświetlenie wybranego węzła
            if (SelectedNode != null)
            {
                var graphNode = gViewer.Graph.FindNode(SelectedNode.Id);
            }

            gViewer.Refresh();
        }
    }

    private void NodeGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedNode != null && gViewer.Graph != null)
        {
            // Resetowanie stylów wszystkich węzłów
            foreach (var node in gViewer.Graph.Nodes)
            {
                node.Attr.LineWidth = 1;
                node.Attr.Color = Microsoft.Msagl.Drawing.Color.Black;
            }

            // Podświetlenie wybranego węzła
            var graphNode = gViewer.Graph.FindNode(SelectedNode.Id);
            if (graphNode != null)
            {
                graphNode.Attr.LineWidth = 5;
                graphNode.Attr.Color = Microsoft.Msagl.Drawing.Color.Black;
            }

            // Odświeżenie widoku grafu
            gViewer.Refresh();
        }
    }


}