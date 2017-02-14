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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Matching_Planar_Maps
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Graph graph = new Graph();
            Graph path = new Graph();

            // Initialize graphs
            // Instantiate a graph
            Vertex v1 = new Vertex(1, 1);
            Vertex v2 = new Vertex(4, 4);
            graph.V.Add(v1);
            graph.V.Add(v2);
            graph.E.Add(v1, v2);

            // Instantiate a graph
            Vertex p1 = new Vertex(1, 2);
            Vertex p2 = new Vertex(4, 2);
            path.V.Add(p1);
            path.V.Add(p2);
            path.E.Add(p1, p2);

            Draw(graph);
            Draw(path);

            float r = 1;
            Vertex c = new Vertex(2, 2);
            Dictionary<float, float[]> freeCell = new Dictionary<float, float[]>();

            Vertex intersection1;
            Vertex intersection2;
            if (FindLineCircleIntersections(c.X, c.Y, r, v1, v2, out intersection1, out intersection2) == 2)
            {
                DrawLine(intersection1, intersection2, Brushes.Gold);
                DrawCircle(new Vertex(c.X, c.Y), r, Brushes.Black);

                float l1 = DistanceSquared(v1, intersection1) / DistanceSquared(v1, v2);
                float l2 = DistanceSquared(v1, intersection2) / DistanceSquared(v1, v2);

                freeCell.Add(DistanceSquared(v1, v2), new []{l1, l2});
                DrawCell(freeCell);

                Console.Out.WriteLine(l1);
                Console.Out.WriteLine(l2);
            }
        }

        public void DrawCell(Dictionary<float, float[]> cell)
        {
            Canvas ccell = new Canvas();
            foreach (KeyValuePair<float, float[]> i in cell)
            {
                Line line = new Line()
                {
                    X1 = i.Key * scale,
                    Y1 = i.Value[0] * scale,
                    X2 = i.Key * scale,
                    Y2 = i.Value[1] * scale
                };
                line.Stroke = Brushes.AliceBlue;
                line.StrokeThickness = 2;
                canvas.Children.Add(line);
            }
        }

        private double scale = 50;
        public void Draw(Graph graph)
        {
            foreach (KeyValuePair<Vertex, Vertex> edge in graph.E)
            {
                DrawLine(edge.Key, edge.Value, Brushes.LightBlue);
            }
        }

        public void DrawLine(Vertex v1, Vertex v2, Brush brush)
        {
            Line line = new Line()
            {
                X1 = v1.X * scale,
                Y1 = v1.Y * scale,
                X2 = v2.X * scale,
                Y2 = v2.Y * scale
            };
            line.Stroke = brush;
            line.StrokeThickness = 2;
            canvas.Children.Add(line);
        }

        public void DrawCircle(Vertex v, float r, Brush brush)
        {
            SolidColorBrush mySolidColorBrush = new SolidColorBrush();

            Ellipse ellipse = new Ellipse()
            {
                Width = 2 * r * scale,
                Height = 2 * r * scale,
            };
            ellipse.Stroke = brush;
            ellipse.StrokeThickness = 1;

            Canvas.SetLeft(ellipse, v.X * scale - r * scale);
            Canvas.SetTop(ellipse, v.Y * scale - r * scale);

            canvas.Children.Add(ellipse);
        }

        public Vertex ClosestIntersection(float cx, float cy, float radius, Vertex lineStart, Vertex lineEnd)
        {
            Vertex intersection1;
            Vertex intersection2;
            int intersections = FindLineCircleIntersections(cx, cy, radius, lineStart, lineEnd, out intersection1, out intersection2);

            if (intersections == 1)
                return intersection1;//one intersection

            if (intersections == 2)
            {
                double dist1 = DistanceSquared(intersection1, lineStart);
                double dist2 = DistanceSquared(intersection2, lineStart);

                if (dist1 < dist2)
                    return intersection1;
                else
                    return intersection2;
            }

            return null;// no intersections at all
        }

        private float DistanceSquared(Vertex v1, Vertex p2)
        {
            return (float)Math.Sqrt(Math.Pow(p2.X - v1.X, 2) + Math.Pow(p2.Y - v1.Y, 2));
        }

        // Find the points of intersection.
        private int FindLineCircleIntersections(float cx, float cy, float radius,
            Vertex point1, Vertex point2, out Vertex intersection1, out Vertex intersection2)
        {
            float dx, dy, A, B, C, det, t;

            dx = point2.X - point1.X;
            dy = point2.Y - point1.Y;

            A = dx * dx + dy * dy;
            B = 2 * (dx * (point1.X - cx) + dy * (point1.Y - cy));
            C = (point1.X - cx) * (point1.X - cx) + (point1.Y - cy) * (point1.Y - cy) - radius * radius;

            det = B * B - 4 * A * C;
            if ((A <= 0.0000001) || (det < 0))
            {
                // No real solutions.
                intersection1 = new Vertex(float.NaN, float.NaN);
                intersection2 = new Vertex(float.NaN, float.NaN);
                return 0;
            }
            else if (det == 0)
            {
                // One solution.
                t = -B / (2 * A);
                intersection1 = new Vertex(point1.X + t * dx, point1.Y + t * dy);
                intersection2 = new Vertex(float.NaN, float.NaN);
                return 1;
            }
            else
            {
                // Two solutions.
                t = (float)((-B + Math.Sqrt(det)) / (2 * A));
                intersection1 = new Vertex(point1.X + t * dx, point1.Y + t * dy);
                t = (float)((-B - Math.Sqrt(det)) / (2 * A));
                intersection2 = new Vertex(point1.X + t * dx, point1.Y + t * dy);
                return 2;
            }
        }

    }
}
