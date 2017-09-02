using System.Windows;

namespace Matching_Planar_Maps
{
    /// <summary>
    /// Interaction logic for GridBuilder.xaml
    /// </summary>
    public partial class GridBuilderWindow : Window
    {
        public GridBuilderWindow()
        {
            InitializeComponent();
        }

        public GridGraph Grid = null;

        private void btn_Generate_Click(object sender, RoutedEventArgs e)
        {
            int width;
            int heigth;
            float cellSize;

            if (int.TryParse(txt_Width.Text, out width) &&
                int.TryParse(txt_Height.Text, out heigth) &&
                float.TryParse(txt_CellSize.Text, out cellSize))
            {
                Grid = new GridGraph(width, cellSize);

                DialogResult = true;
                Close();
            }
        }
    }
}
