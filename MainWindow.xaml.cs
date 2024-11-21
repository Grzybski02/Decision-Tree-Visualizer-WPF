using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
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
    GViewer gViewer;
    public MainWindow()
    {
        InitializeComponent();
        GrapherSKL grapher = new GrapherSKL();

        graphHost.Child = gViewer;
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedFormat = formatSelector.SelectedIndex;
        if (selectedFormat < 0)
        {
            System.Windows.MessageBox.Show("Please select a format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        GrapherSKL grapher = new GrapherSKL();
        GViewer gViewer = null;
        string filePath;

        switch (selectedFormat)
        {
            case 0:
                filePath = SelectFileWithExtension("Log files (*.log)|*.log");
                if (filePath == null) return;
                gViewer = grapher.RenderDecisionTree(filePath, GrapherSKL.TreeFormat.Graphviz);
                break;
            case 1:
                filePath = SelectFileWithExtension("Text files (*.txt)|*.txt");
                if (filePath == null) return;
                gViewer = grapher.RenderDecisionTree(filePath, GrapherSKL.TreeFormat.MLPDT);
                break;
            default:
                System.Windows.MessageBox.Show("Invalid format selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
        }

        graphHost.Child = gViewer;
    }

    private string SelectFileWithExtension(string filter)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = filter;
        openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();

        if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            return openFileDialog.FileName;
        }

        return null;
    }
}