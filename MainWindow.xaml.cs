using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Decision_Trees_Visualizer.Services;
using Microsoft.Msagl.GraphViewerGdi;

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

        gViewer = new GViewer();
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

    private void ExportJsonMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ExportToJson();
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

        var format = fileService.SelectFormatDialog();
        if (format == null) return;

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

        nodeGraphManager.RenderGraph(Nodes.ToList());

        // Dodaj do ostatnich plików
        fileService.AddToRecentFiles(result, format);
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
            grid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            grid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
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
        RecentFilesMenu.Items.Clear();

        if (recentFiles.Count == 0)
        {
            RecentFilesMenu.Items.Add(new MenuItem { Header = "(None)", IsEnabled = false });
            return;
        }

        foreach (var (filePath, format) in recentFiles)
        {
            var menuItem = new MenuItem
            {
                Header = System.IO.Path.GetFileName(filePath),
                ToolTip = $"{filePath} ({format})"
            };
            menuItem.Click += (sender, args) =>
            {
                var nodes = fileService.LoadFile(filePath, format);
                if (nodes == null || nodes.Count == 0)
                {
                    System.Windows.MessageBox.Show("No nodes found in the selected file.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Nodes.Clear();
                foreach (var n in nodes)
                {
                    if (string.IsNullOrEmpty(n.ColorName))
                        n.ColorName = "White";
                    Nodes.Add(n);
                }

                nodeGraphManager.RenderGraph(Nodes.ToList());
            };
            RecentFilesMenu.Items.Add(menuItem);
        }
    }

}