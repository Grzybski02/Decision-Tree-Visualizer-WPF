using System.Windows;

namespace Decision_Trees_Visualizer;
/// <summary>
/// Logika interakcji dla klasy EditNodeWindow.xaml
/// </summary>
public partial class EditNodeWindow : Window
{
    public Node Node { get; set; }
    public List<string> ColorNames { get; set; }

    public EditNodeWindow(Node node, List<string> colorNames)
    {
        InitializeComponent();
        Node = node;
        ColorNames = colorNames;
        DataContext = this; // Set DataContext to the window
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
