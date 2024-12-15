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

    public void Initialize(ObservableCollection<Node> nodes, WindowsFormsHost host, GViewer viewer)
    {
        Nodes = nodes;
        graphHost = host;
        gViewer = viewer;
        gViewer.MouseDoubleClick += GViewer_MouseDoubleClick;
    }

    public void RenderGraph(List<Node> nodeList)
    {
        if (gViewer == null)
            throw new InvalidOperationException("GViewer is not initialized.");

        var grapher = new GrapherSKL();
        gViewer.Graph = grapher.RenderDecisionTree(nodeList).Graph;
        gViewer.Refresh();
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
                EditNodeProperties(correspondingNode);
            }
        }
    }

    private void EditNodeProperties(Node node)
    {
        var editWindow = new EditNodeWindow(node, new ColorList().GetPredefinedColorNames());
        var result = editWindow.ShowDialog();

        if (result == true)
        {
            UpdateGraphAfterNodeEdit(node);

            var graphNode = gViewer.Graph.FindNode(node.Id);
            ResetNodeOutline(graphNode);
        }
    }

    private void UpdateGraphAfterNodeEdit(Node editedNode)
    {
        if (gViewer == null || gViewer.Graph == null)
            return;

        var graphNode = gViewer.Graph.FindNode(editedNode.Id);
        if (graphNode != null)
        {
            // Aktualizuj etykietę i kolor węzła
            graphNode.LabelText = editedNode.Label;
            if (editedNode.IsClassLeaf)
            {
                graphNode.Attr.FillColor = new ColorList().GetColorByName(editedNode.ColorName);
            }

            // Resetuj obrys
            ResetNodeOutline(graphNode);
        }

        gViewer.Refresh();
    }


    private void ResetNodeOutline(Microsoft.Msagl.Drawing.Node graphNode)
    {
        if (graphNode == null) return;

        graphNode.Attr.LineWidth = 1.0;

        graphNode.Attr.Color = Microsoft.Msagl.Drawing.Color.Black;
    }

}
