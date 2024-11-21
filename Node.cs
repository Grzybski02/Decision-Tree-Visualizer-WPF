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
    public List<Node> Children { get; set; } = new List<Node>();
}
