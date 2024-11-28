using Microsoft.Msagl.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decision_Trees_Visualizer;
internal class ColorList
{
    private List<string> colorNames;

    public ColorList()
    {
        colorNames = GetPredefinedColorNames();
    }

    internal List<string> GetPredefinedColorNames()
    {
        return new List<string>
        {
            "Aquamarine", "Beige","CadetBlue", "Coral","CornflowerBlue", 
            "DarkCyan", "DarkGray", "DarkKhaki","DarkOliveGreen", 
            "DarkRed", "DarkSeaGreen", "DarkSlateBlue", "DarkSlateGray", "DarkTurquoise", "DimGray", "DodgerBlue", "Firebrick", 
            "ForestGreen", "GreenYellow","HotPink",
            "LemonChiffon", "LightBlue", "LightGoldenrodYellow", "LightGreen", "LightPink",
            "LightSeaGreen", "LightSkyBlue", "LightSlateGray",
            "LightSteelBlue", "LimeGreen", "Maroon", "MediumAquamarine", "MediumOrchid",
            "MediumPurple", "MediumSeaGreen", "MediumSlateBlue", "MediumSpringGreen",
            "MediumTurquoise", "MistyRose", "Moccasin", "PaleGoldenrod",
            "PaleGreen", "PaleTurquoise", "PaleVioletRed", "PapayaWhip",
            "PeachPuff", "Peru", "Pink", "Plum", "PowderBlue", "RosyBrown", "Salmon", "SandyBrown",
            "SeaGreen", "Silver", "SkyBlue", "SlateBlue",
            "SpringGreen", "SteelBlue", "Tan", "Teal",
            "Thistle", "Tomato", "Turquoise", "Violet", "Wheat", "YellowGreen"
        };
    }

    public Color GetColorByName(string colorName)
    {
        // Używamy refleksji, aby uzyskać statyczną właściwość Color o nazwie colorName
        var colorProperty = typeof(Color).GetProperty(colorName);
        if (colorProperty != null)
        {
            return (Color)colorProperty.GetValue(null);
        }
        else
        {
            // Jeśli kolor nie istnieje, zwróć domyślny kolor (np. Czarny)
            return Color.PaleTurquoise;
        }
    }

    public (Color color, string colorName) GetColorForClass(string className)
    {
        int hash = className.GetHashCode();
        hash = Math.Abs(hash);
        int colorIndex = hash % colorNames.Count;
        string colorName = colorNames[colorIndex];
        Color color = GetColorByName(colorName);
        return (color, colorName);
    }

}

