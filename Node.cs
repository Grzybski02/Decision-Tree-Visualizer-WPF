using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decision_Trees_Visualizer;

public class Node : INotifyPropertyChanged
{
    public string Id { get; set; }

    private string label;
    public string Label
    {
        get => label;
        set
        {
            label = value;
            OnPropertyChanged(nameof(Label));
        }
    }

    private string colorName;
    public string ColorName
    {
        get => colorName;
        set
        {
            colorName = value;
            OnPropertyChanged(nameof(ColorName));
        }
    }

    public bool IsClassLeaf { get; set; } // Stała wartość określająca, czy węzeł jest liściem klasy

    public int? Depth { get; set; } // Poziom w drzewie
    public int? LeftChildIndex { get; set; } // Indeks lewego dziecka w liście
    public int? RightChildIndex { get; set; } // Indeks prawego dziecka w liście
    public string LeftEdgeLabel { get; set; } // Etykieta lewej krawędzi
    public string RightEdgeLabel { get; set; } // Etykieta prawej krawędzi

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}





