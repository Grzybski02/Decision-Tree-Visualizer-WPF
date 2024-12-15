using Microsoft.Msagl.GraphViewerGdi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;

namespace Decision_Trees_Visualizer.Services;
public class NodeGraphManager
{
    private GViewer gViewer;
    private ObservableCollection<Node> Nodes;
    private WindowsFormsHost graphHost;

    public void Initialize(ObservableCollection<Node> nodes, WindowsFormsHost host)
    {
        Nodes = nodes;
        graphHost = host;
    }

    public void RenderGraph(System.Collections.Generic.List<Node> nodeList)
    {
        if (gViewer == null)
        {
            gViewer = new GViewer();
            gViewer.MouseDoubleClick += GViewer_MouseDoubleClick;
            graphHost.Child = gViewer;
        }

        // Załóżmy, że GrapherSKL posiada metodę RenderDecisionTree
        var grapher = new GrapherSKL();
        gViewer = grapher.RenderDecisionTree(nodeList);
        graphHost.Child = gViewer;
    }

    public void Nodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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
        if (gViewer == null || gViewer.Graph == null)
            return;

        var editedNode = sender as Node;
        if (editedNode == null) return;

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
                // Wymuszenie odświeżenia
                gViewer.Graph = gViewer.Graph;
            }

            gViewer.Refresh();
        }
    }

    public void NodeGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
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

        if (gViewer == null || gViewer.Graph == null)
            return;

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

        gViewer.Refresh();
    }

    private void GViewer_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
    {
        var objectUnderMouseCursor = gViewer.ObjectUnderMouseCursor;

        if (objectUnderMouseCursor != null && objectUnderMouseCursor.DrawingObject is Microsoft.Msagl.Drawing.Node graphNode)
        {
            string nodeId = graphNode.Id;
            var correspondingNode = Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (correspondingNode != null)
            {
                // Znajdź główne okno (rzuć w górę VisualTree jeśli trzeba)
                var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                if (mainWindow != null)
                {
                    mainWindow.NodeGrid.SelectedItem = correspondingNode;
                    mainWindow.NodeGrid.ScrollIntoView(correspondingNode);
                    EditNodeProperties(correspondingNode, mainWindow);
                }
            }
        }
    }

    private void EditNodeProperties(Node node, MainWindow owner)
    {
        var editWindow = new EditNodeWindow(node, new ColorList().GetPredefinedColorNames());
        editWindow.Owner = owner;
        var result = editWindow.ShowDialog();

        if (result == true)
        {
            // Odśwież
            owner.NodeGrid.Items.Refresh();
        }
    }
}
