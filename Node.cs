using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decision_Trees_Visualizer;

public class Node
{
    public string Id { get; set; }
    public string Label { get; set; }
    public int? Depth { get; set; } // Poziom w drzewie
    public int? LeftChildIndex { get; set; } // Indeks lewego dziecka w liście
    public int? RightChildIndex { get; set; } // Indeks prawego dziecka w liście
    public string LeftEdgeLabel { get; set; } // Etykieta lewej krawędzi
    public string RightEdgeLabel { get; set; } // Etykieta prawej krawędzi
}



