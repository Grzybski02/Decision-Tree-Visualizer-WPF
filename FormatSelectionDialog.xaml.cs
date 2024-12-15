using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Decision_Trees_Visualizer;
    public partial class FormatSelectionDialog : Window
    {
        public string SelectedFormat { get; private set; }

        public FormatSelectionDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (GraphvizOption.IsChecked == true)
                SelectedFormat = "Graphviz";
            else if (MLPDTOption.IsChecked == true)
                SelectedFormat = "MLPDT";
            else if (JSONOption.IsChecked == true)
                SelectedFormat = "JSON";

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
