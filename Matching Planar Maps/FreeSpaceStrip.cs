using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Matching_Planar_Maps
{
    public class FreeSpaceStrip : Grid
    {
        public float _size = 100;
        public int I = -1;
        public int J = -1;
        public Canvas Canvas;
        public bool active = false;
        public WriteableBitmap wbmp;
        public Image imgControl;

        public FreeSpaceStrip(int i, int j, float size)
        {
            this.I = i;
            this.J = j;
            this._size = size;

            this.Height = _size;
            this.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(40, GridUnitType.Pixel) });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            Canvas = new Canvas() { 
                Height = _size,
                Background = Brushes.Gray,
                LayoutTransform = new ScaleTransform(1, -1, .5, .5),
                
            };
            Grid.SetColumn(Canvas, 1);
            //

            imgControl = new Image();
            imgControl.HorizontalAlignment = HorizontalAlignment.Left;
            Grid.SetColumn(imgControl, 1);
            this.Children.Add(imgControl);

            // Add labels
            Grid labelGrid = new Grid();
            labelGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            labelGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            labelGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            Grid.SetColumn(labelGrid, 0);
            this.Children.Add(labelGrid);

            Label lbl1 = new Label()
            {
                Content = "v" + I
            };
            Grid.SetRow(lbl1, 2);
            labelGrid.Children.Add(lbl1);
            Label lbl2 = new Label()
            {
                Content = "v" + J
            };
            Grid.SetRow(lbl2, 0);
            labelGrid.Children.Add(lbl2);
        }

    }
}
