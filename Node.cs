using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Decision_Trees_Visualizer;

public class Node : INotifyPropertyChanged
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("label")]
    private string label;
    public string Label
    {
        get => label;
        set
        {
            if (label != value)
            {
                label = value;
                OnPropertyChanged(nameof(Label));
            }
        }
    }

    [JsonPropertyName("color")]
    private string colorName;
    public string ColorName
    {
        get => colorName;
        set
        {
            if (colorName != value)
            {
                colorName = value;
                OnPropertyChanged(nameof(ColorName));
            }
        }
    }

    [JsonPropertyName("is_leaf")]
    public bool IsClassLeaf { get; set; } // Stała wartość określająca, czy węzeł jest liściem klasy

    [JsonPropertyName("depth")]
    public int? Depth { get; set; } // Poziom w drzewie

    [JsonPropertyName("left_child")]
    public int? LeftChildIndex { get; set; } // Indeks lewego dziecka w liście

    [JsonPropertyName("right_child")]
    public int? RightChildIndex { get; set; } // Indeks prawego dziecka w liście

    [JsonPropertyName("test_info")]
    public string TestInfo { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class GraphvizNode : Node
{
    [JsonPropertyName("left_label")]
    public string? LeftEdgeLabel { get; set; } // Indeks lewego dziecka w liście

    [JsonPropertyName("right_label")]
    public string? RightEdgeLabel { get; set; } // Indeks prawego dziecka w liście

    [JsonPropertyName("test")]
    public string Test { get; set; }       // Test decyzyjny (np. petal length <= 2.45)
    
    [JsonPropertyName("gini")]
    public double Gini { get; set; }       // Wartość Gini

    [JsonPropertyName("samples")]
    public int Samples { get; set; }       // Liczba próbek

    [JsonPropertyName("value")]
    public List<int> Value { get; set; }   // Wartości klas

    [JsonPropertyName("classname")]
    public string ClassName { get; set; }  // Nazwa klasy

}




