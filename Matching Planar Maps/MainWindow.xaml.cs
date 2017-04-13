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
        private Graph graph;
        private Graph path;

        private const float TOLERANCE = 0.0000001f;

        public float epsilon { get; set; } = 50;

        private Interval[,] B;
        private Interval[,] Bp;
        private Interval[,] L;
        private Interval[,] Lp;

        private float[,] leftPointers;
        private float[,] rightPointers;

        private Boolean initialized = false;

        public MainWindow()
        {
            InitializeComponent();


        }

        public void init()
        {
            graph = new Graph();

            // Initialize graphs
            // Instantiate a graph
            Vertex v1 = new Vertex(20, 20);
            Vertex v2 = new Vertex(80, 80);
            Vertex v3 = new Vertex(110, 30);
            Vertex v4 = new Vertex(150, 50);
            Vertex v5 = new Vertex(240, 80);
            graph.V.Add(v1);
            graph.V.Add(v2);
            graph.V.Add(v3);
            graph.V.Add(v4);
            graph.E.Add(new Edge(v1, v2));
            graph.E.Add(new Edge(v2, v3));
            graph.E.Add(new Edge(v3, v4));
            //graph.E.Add(new Edge(v2, v5));

            // The path
            path = new Graph();
            Vertex pv1 = new Vertex(20, 60);
            Vertex pv2 = new Vertex(80, 30);
            Vertex pv3 = new Vertex(120, 60);
            Vertex pv4 = new Vertex(150, 60);
            path.V.Add(pv1);
            path.V.Add(pv2);
            path.E.Add(new Edge(pv1, pv2));
            path.E.Add(new Edge(pv2, pv3));
            path.E.Add(new Edge(pv3, pv4));

            canvas.Children.Clear();

            Draw(graph, Brushes.Turquoise);
            Draw(path, Brushes.GreenYellow);
        }

        private Ellipse sizeIndicator = null;
        public void Calc()
        {
            canvas.Children.Remove(sizeIndicator);
            sizeIndicator = DrawCircle(path.V[1], epsilon, Brushes.Chocolate);

            L = new Interval[path.E.Count, graph.E.Count];
            Lp = new Interval[path.E.Count, graph.E.Count];
            B = new Interval[path.E.Count, graph.E.Count];
            Bp = new Interval[path.E.Count, graph.E.Count];
            leftPointers = new float[path.E.Count, graph.E.Count];
            rightPointers = new float[path.E.Count, graph.E.Count];

            // Set all values to nan
            for (int i = 0; i < path.E.Count; i++)
            {
                for (int n = 0; n < graph.E.Count; n++)
                {
                    leftPointers[i, n] = float.NaN;
                    rightPointers[i, n] = float.NaN;
                }
            }


            int steps = 100;
            double Size = 100;

            freeSpaceStack.Children.Clear();
            for (int b = graph.E.Count - 1; b > -1; b--)
            {
                Edge e2 = graph.E[b];

                Canvas freeSpaceStrip = new Canvas() { Height = 100 };
                freeSpaceStrip.Background = Brushes.Gray;
                freeSpaceStrip.LayoutTransform = new ScaleTransform(1, -1, .5, .5);
                freeSpaceStack.Children.Add(freeSpaceStrip);

                for (int n = 0; n < path.E.Count; n++)
                {
                    Edge e = path.E[n];

                    // Draw free space cell
                    for (int i = 0; i < steps; i++)
                    {
                        float loc = ((1f / steps) * i);

                        Interval interval = CalculateInterval(e, e2, loc, epsilon);
                        //Console.WriteLine(String.Format("{0}, {1}", interval.Start, interval.End));
                        if (!interval.isEmpty())
                        {
                            Line line = new Line()
                            {
                                X1 = (loc + n) * Size,
                                Y1 = interval.Start * Size,
                                X2 = (loc + n) * Size,
                                Y2 = interval.End * Size,
                                Stroke = Brushes.White,
                                StrokeThickness = 1
                            };
                            freeSpaceStrip.Children.Add(line);
                        }
                    }
                }

                for (int n = 0; n < path.E.Count; n++)
                {
                    Edge e = path.E[n];

                    // Calculate LK and BK
                    Interval LK = CalculateInterval(e, e2, 0, epsilon);
                    LK.PathIndex = n;
                    LK.GraphIndex = b;
                    L[n, b] = LK;
                    if (!LK.isEmpty())
                    {
                        Line lkLine = new Line()
                        {
                            X1 = n * Size,
                            Y1 = LK.Start * Size,
                            X2 = n * Size,
                            Y2 = LK.End * Size,
                            Stroke = Brushes.Green,
                            StrokeThickness = 2
                        };
                        //freeSpaceStrip.Children.Add(lkLine);
                    }

                    Interval LKa = CalculateInterval(e, e2, 1, epsilon);
                    LKa.PathIndex = n;
                    LKa.GraphIndex = b;
                    Lp[n, b] = LKa;
                    if (!LKa.isEmpty())
                    {
                        Line lkLine = new Line()
                        {
                            X1 = (n + 1) * Size,
                            Y1 = LKa.Start * Size,
                            X2 = (n + 1) * Size,
                            Y2 = LKa.End * Size,
                            Stroke = Brushes.Blue,
                            StrokeThickness = 2
                        };
                        //freeSpaceStrip.Children.Add(lkLine);
                    }

                    Interval BK = CalculateInterval(e2, e, 0f, epsilon);
                    BK.PathIndex = n;
                    BK.GraphIndex = b;
                    B[n, b] = BK;
                    if (!BK.isEmpty())
                    {
                        Line bkLine = new Line()
                        {
                            X1 = (BK.Start + n) * Size,
                            Y1 = 0,
                            X2 = (BK.End + n) * Size,
                            Y2 = 0,
                            Stroke = Brushes.Blue,
                            StrokeThickness = 2
                        };
                        freeSpaceStrip.Children.Add(bkLine);
                    }

                    Interval BKa = CalculateInterval(e2, e, 1, epsilon);
                    BKa.PathIndex = n;
                    BKa.GraphIndex = b;
                    Bp[n, b] = BKa;
                    if (!BKa.isEmpty())
                    {
                        Line lkLine = new Line()
                        {
                            X1 = (BKa.Start + n) * Size,
                            Y1 = Size,
                            X2 = (BKa.End + n) * Size,
                            Y2 = Size,
                            Stroke = Brushes.Red,
                            StrokeThickness = 2
                        };
                        freeSpaceStrip.Children.Add(lkLine);
                    }
                }

                calcLeftPointers(b);

                // Draw leftpointers
                for (int n = 0; n < path.E.Count; n++)
                {
                    if (!float.IsNaN(leftPointers[n, b]))
                    {

                        Line leftpointerLine = new Line()
                        {
                            X1 = (B[n, b].Start + n) * Size,
                            Y1 = 0,
                            X2 = (leftPointers[n, b]) * Size,
                            Y2 = Size,
                            Stroke = Brushes.Red,
                            StrokeThickness = 1
                        };
                        freeSpaceStrip.Children.Add(leftpointerLine);
                    }
                }

                calcRightPointers(b);
                // Draw rightpointers
                for (int n = 0; n < path.E.Count; n++)
                {
                    if (!B[n, b].isEmpty())
                    {
                        if (!float.IsNaN(rightPointers[n, b]))
                        {
                            Line rightpointerLine = new Line()
                            {
                                X1 = (B[n, b].Start + n) * Size,
                                Y1 = 0,
                                X2 = (rightPointers[n, b]) * Size,
                                Y2 = Size,
                                Stroke = Brushes.Green,
                                StrokeThickness = 1
                            };
                            freeSpaceStrip.Children.Add(rightpointerLine);
                        }
                    }
                }
            }

            dynamicProgrammingStage();
        }

        public void calcLeftPointers(int j)
        {

            for (int k = 0; k < path.E.Count; k++)
            {
                leftPointers[k, j] = float.NaN;
                float max_ai = L[k, j].Start;

                if (B[k, j].Start > B[k, j].End)  // It has no opening
                {
                    continue;
                }

                if (B[k, j].Start <= Bp[k, j].End && Bp[k, j].End >= Bp[k, j].Start)
                {
                    leftPointers[k, j] = k + Math.Max(B[k, j].Start, Bp[k, j].Start);
                }
                else
                {
                    for (int n = k + 1; n < path.E.Count; n++)
                    {
                        if (L[n, j].End >= max_ai) // Can move to this cell in a monetone path
                        {
                            if (Bp[n, j].Start > Bp[n, j].End)
                            {
                                max_ai = Math.Max(L[n, j].Start, max_ai);
                            }
                            else
                            {
                                leftPointers[k, j] = Bp[n, j].Start + n;
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }


        public void calcRightPointers(int j)
        {
            List<int> S = new List<int>();
            int kp = 1;
            float ai1 = 0f;
            S.Add(0);

            for (int k = 0; k < path.E.Count; k++)
            {
                kp = k;

                while (kp < path.E.Count)
                {
                    if (ai1 > Lp[kp, j].End || kp + 1 == path.E.Count || Lp[kp, j].isEmpty())
                    {
                        // Maximal kp that fulfills (1)
                        int w = kp;

                        // Search with point to the left of kp + 1;
                        while (w > 0)
                        {
                            if (!Bp[w, j].isEmpty())
                            {
                                break;
                            }
                            w--;
                        }

                        if (k < w)
                        {
                            rightPointers[k, j] = w + Bp[w, j].End;
                        }
                        else if (k > w)
                        {
                            rightPointers[k, j] = float.NaN;
                        }
                        else
                        {
                            if (B[k, j].Start <= Bp[k, j].End)
                            {
                                rightPointers[k, j] = k + Bp[k, j].End;
                            }
                            else
                            {
                                rightPointers[k, j] = float.NaN;
                            }
                        }

                        // Remove bottom element of queue
                        if (S.Any() && S.First() == k + 1)
                        {
                            S.RemoveAt(0);
                            if (!S.Any())
                            {
                                ai1 = 0f;
                            }
                        }

                        break;
                    }
                    else
                    {
                        kp++;

                        // Pop topmost values from S until aim > ak'
                        while (S.Any() && L[S.Last(), j].Start > L[kp, j].Start)
                        {
                            S.Remove(S.Last());
                            if (!S.Any())
                            {
                                ai1 = 0f;
                            }
                        }
                        S.Add(kp);
                    }
                }
            }
        }

        public void dynamicProgrammingStage()
        {
            //////////////////////////
            // Initialization phase //
            //////////////////////////
            List<Interval> Q = new List<Interval>();
            Interval[] C = new Interval[graph.E.Count];
            Interval result = null;
            float x = 0; 

            // Initialize Q
            for (int i = 0; i < graph.E.Count; i++)
            {
                if (!B[0, i].isEmpty() && Math.Abs(B[0, i].Start) < TOLERANCE)
                {
                    Q.Add(B[0, i]);
                    C[i] = new Interval(B[0, i].Start, B[0,i].End);
                }
                else
                {
                    C[i] = null;
                }
            }

            // Init Q
            Console.WriteLine("-- Init Q --");
            for (int i = 0; i < Q.Count; i++)
            {
                Interval interval = Q[i];
                Console.WriteLine(String.Format("{0}: {1}, l: {2:0.0000} r: {3:0.0000}", i, interval.PathIndex + interval.Start, leftPointers[interval.PathIndex, interval.GraphIndex], rightPointers[interval.PathIndex, interval.GraphIndex]));
            }

            Console.WriteLine("-- Init Ci --");
            for (int i = 0; i < C.Length; i++)
            {
                if (C[i] != null)
                {
                    Console.WriteLine(String.Format("{0}: l: {1:0.0000} r: {2:0.0000}", i, C[i].Start, C[i].End));
                }
            }

            // If q is not empty continue otherwise no path exists
            while (Q.Any() && result == null)
            {
                // Sort q to priority
                Q = Q.OrderBy(q => q.Start).ToList();

                // Step 1 extract leftmost interval
                Interval I = Q.First(); // Get first interval
                Q.Remove(Q.First()); // Remove fist interval
                x = leftPointers[I.PathIndex, I.GraphIndex]; // Advance x to l(I)
                Console.WriteLine("x: " + x);
                if (float.IsNaN(x))
                {
                    continue;
                }

                
                

                // Step 2 
                // Insert the next white interval of Ci which lies to right of I into Q
                for (int i = I.PathIndex + 1; i < path.E.Count; i++) // Search to the right for first white interval
                {
                    if (!B[i, I.GraphIndex].isEmpty()) // First not empty white interval
                    {
                        Q.Add(B[i, I.GraphIndex]);
                        Q = Q.OrderBy(q => q.Start).ToList(); // Should be log n
                        break;
                    }
                }

                // Step 3 / 4
                // Find all adjacent edges
                List<Edge> adj = graph.E.Where(e => e.V1 == graph.E[I.GraphIndex].V2).ToList();
                foreach (Edge e in adj)
                {
                    int index = graph.E.IndexOf(e);
                    Interval cj = C[index];
                    if (!float.IsNaN(leftPointers[I.PathIndex, I.GraphIndex]) && cj != null)
                    {
                        if (leftPointers[I.PathIndex, I.GraphIndex] > cj.End)
                        {
                            cj.Start = leftPointers[I.PathIndex, I.GraphIndex];
                            cj.End = rightPointers[I.PathIndex, I.GraphIndex];
                            cj.PathPointer = I;
                        }
                    }

                    // If left point changed delete old interval of cj in q
                    if (cj == null || Math.Abs(leftPointers[I.PathIndex, I.GraphIndex] - leftPointers[cj.PathIndex, cj.GraphIndex]) > TOLERANCE)
                    {
                        // Remove old cj
                        Q.Remove(cj);

                        int start = cj == null ? 0 : cj.PathIndex;
                        // Add new cj to the right
                        for (int i = start; i < path.E.Count; i++)
                        {
                            if (B[i, index] != null && !B[i, index].isEmpty())
                            {
                                    cj = B[i, index];
                                    cj.PathPointer = I;
                                    Q.Add(cj);
                                    break;
                            }
                        }
                    }

                    // Set result if endpoint reached
                    if (cj != null)
                    {
                        Console.WriteLine("Rightpointer values: " + rightPointers[cj.PathIndex, cj.GraphIndex]);
                        if (Math.Abs(rightPointers[cj.PathIndex, cj.GraphIndex] - path.E.Count) < TOLERANCE)
                        {
                            result = C[graph.E.IndexOf(e)];
                            result.PathPointer = I;
                        }
                    }

                }

                // Print q
                Console.WriteLine("-- Show Q --");
                for (int i = 0; i < Q.Count; i++)
                {
                    Interval interval = Q[i];
                    Console.WriteLine(interval.PathIndex + interval.Start);
                }
            }

            // Clear old result drawing
            foreach (Line resultLine in resultPath)
            {
                canvas.Children.Remove(resultLine);
            }
            resultPath.Clear();

            if (result != null)
            {
                Console.WriteLine("Feasable path exists");
                while (result != null)
                {
                    resultPath.Add(DrawLine(graph.E[result.GraphIndex].V1, graph.E[result.GraphIndex].V2, Brushes.Blue));
                    result = result.PathPointer;
                }
            }
            else
            {
                Console.WriteLine("No feasable path exists");
            }

        }

        public List<Line> resultPath = new List<Line>();

        public void DrawQ(List<Interval> Q)
        {
            foreach (Interval q in Q)
            {
                Ellipse circle = createCircle(new Vertex(q.Start, 0), 20);
                circle.Fill = Brushes.Brown;
            }
        }

        public void Draw(Graph graph, Brush brush)
        {
            foreach (Edge edge in graph.E)
            {
                DrawLine(edge.V1, edge.V2, brush);
            }
        }

        public Interval CalculateInterval(Edge e1, Edge e2, float location, float epsilon)
        {
            Vertex c = e1.Location(location);
            Vertex intersection1;
            Vertex intersection2;
            int nrOfIntersections = FindLineCircleIntersections(c.X, c.Y, epsilon, e2.V1, e2.V2, out intersection1,
                out intersection2);

            Interval interval = new Interval(1, 0);
            if (nrOfIntersections == 2)
            {
                // If both points are outside range return empty intervall
                if (DistanceSquared(e2.V1, new Vertex(c.X, c.Y)) < Math.Pow(epsilon, 2))
                {
                    intersection2.X = e2.V1.X;
                    intersection2.Y = e2.V1.Y;
                }
                else if (DistanceSquared(e2.V2, new Vertex(c.X, c.Y)) < Math.Pow(epsilon, 2))
                {
                    intersection1.X = e2.V2.X;
                    intersection1.Y = e2.V2.Y;
                }


                interval.Start = e2.Location(intersection2);
                interval.End = e2.Location(intersection1);
            }
            return interval;
        }

        public void DrawEdge(Edge e, Brush brush)
        {
            Line line = new Line()
            {
                X1 = e.V1.X,
                Y1 = e.V1.Y,
                X2 = e.V2.X,
                Y2 = e.V2.Y
            };
            line.Stroke = brush;
            line.StrokeThickness = 2;
            canvas.Children.Add(line);
        }

        public Line DrawLine(Vertex v1, Vertex v2, Brush brush)
        {
            Line line = new Line()
            {
                X1 = v1.X,
                Y1 = v1.Y,
                X2 = v2.X,
                Y2 = v2.Y
            };
            line.Stroke = brush;
            line.StrokeThickness = 2;
            canvas.Children.Add(line);
            return (line);
        }

        public Ellipse DrawCircle(Vertex v, float r, Brush brush)
        {
            Ellipse ellipse = createCircle(v, r);
            ellipse.Stroke = brush;
            ellipse.StrokeThickness = 1;

            if (!canvas.Children.Contains(ellipse))
            {
                canvas.Children.Add(ellipse);
            }

            return ellipse;
        }

        public Ellipse createCircle(Vertex v, float r)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Width = 2 * r;
            ellipse.Height = 2 * r;
            Canvas.SetLeft(ellipse, v.X - r);
            Canvas.SetTop(ellipse, v.Y - r);

            return ellipse;
        }

        private List<Line> intersectionLines = new List<Line>();
        public void DrawIntersectionLine(Vertex v1, Vertex v2, Brush brush)
        {
            if (float.IsNaN(v1.X) || float.IsNaN(v1.Y) || float.IsNaN(v2.X) || float.IsNaN(v2.Y)) return;

            Line intersectionLine = new Line();
            intersectionLine.X1 = v1.X;
            intersectionLine.Y1 = v1.Y;
            intersectionLine.X2 = v2.X;
            intersectionLine.Y2 = v2.Y;
            intersectionLine.Stroke = brush;
            intersectionLine.StrokeThickness = 2;

            canvas.Children.Add(intersectionLine);
            intersectionLines.Add(intersectionLine);
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

        private float DistanceSquared(Vertex v1, Vertex v2)
        {
            return (float)(Math.Pow(v2.X - v1.X, 2) + Math.Pow(v2.Y - v1.Y, 2));
        }


        private void generateGrid(float cellspacing)
        {
            graph = new Graph();
            Graph reverseGraph = new Graph();
            int xSize = 10;
            int ySize = 10;

            // Create vertices
            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                   Vertex v = new Vertex(x * cellspacing, y * cellspacing);

                    // connect to left
                    if (x > 0)
                    {
                        graph.E.Add(new Edge(graph.V.Last(), v));
                        //reverseGraph.E.Add(new Edge(v, graph.V.Last()));
                    }

                    // Connect to above
                    if (y > 0)
                    {
                        graph.E.Add(new Edge(graph.V[graph.V.Count - xSize], v));
                        //reverseGraph.E.Add(new Edge(v, graph.V[graph.V.Count - xSize]));
                    }
                    graph.V.Add(v);
                }
            }

            graph.E.AddRange(reverseGraph.E);

        }

        private float Distance(Vertex v1, Vertex v2)
        {
            return (float)Math.Sqrt(DistanceSquared(v1, v2));
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

            if ((A <= TOLERANCE) || (det < 0))
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

                // Validate that this solution is on the line
                if (Distance(point1, intersection1) + Distance(point2, intersection1) > Distance(point1, point2))
                {
                    intersection1 = new Vertex(float.NaN, float.NaN);
                    return 0;
                }
                return 1;
            }
            else
            {
                // Two solutions.
                t = (float)((-B + Math.Sqrt(det)) / (2 * A));
                intersection1 = new Vertex(point1.X + t * dx, point1.Y + t * dy);
                t = (float)((-B - Math.Sqrt(det)) / (2 * A));
                intersection2 = new Vertex(point1.X + t * dx, point1.Y + t * dy);

                float test1 = Distance(point1, new Vertex(cx, cy));
                float test2 = Distance(point2, new Vertex(cx, cy));

                // Validate that this solution is on the line
                if (Distance(point1, new Vertex(cx, cy)) <= radius * (1 + TOLERANCE) && Distance(point2, new Vertex(cx, cy)) <= radius * (1 + TOLERANCE))
                    return 2;

                if (Distance(point1, intersection1) + Distance(point2, intersection1) > Distance(point1, point2) * (1 + TOLERANCE) &&
                    Distance(point1, intersection2) + Distance(point2, intersection2) > Distance(point1, point2) * (1 + TOLERANCE))
                {
                    intersection1 = new Vertex(float.NaN, float.NaN);
                    intersection2 = new Vertex(float.NaN, float.NaN);
                    return 0;
                }

                return 2;
            }
        }

        private Ellipse circleSelector = null;
        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = Mouse.GetPosition(canvas);
            Vertex c = new Vertex((float)p.X, (float)p.Y);

            if (circleSelector != null)
            {
                canvas.Children.Remove(circleSelector);
            }

            // Remove all intersections
            foreach (Line line in intersectionLines)
            {
                canvas.Children.Remove(line);
            }

            intersectionLines.Clear();

            circleSelector = DrawCircle(c, epsilon, Brushes.Aquamarine);

            Vertex intersection1;
            Vertex intersection2;
            foreach (Edge edge in graph.E)
            {
                int nrOfIntersections = FindLineCircleIntersections(c.X, c.Y, epsilon, edge.V1, edge.V2, out intersection1, out intersection2);

                if (nrOfIntersections == 2)
                {
                    if (DistanceSquared(edge.V1, new Vertex(c.X, c.Y)) < Math.Pow(epsilon, 2))
                    {
                        intersection2.X = edge.V1.X;
                        intersection2.Y = edge.V1.Y;
                    }

                    if (DistanceSquared(edge.V2, new Vertex(c.X, c.Y)) < Math.Pow(epsilon, 2))
                    {
                        intersection1.X = edge.V2.X;
                        intersection1.Y = edge.V2.Y;
                    }
                }

                DrawIntersectionLine(intersection1, intersection2, Brushes.Yellow);
            }

            dynamicProgrammingStage();
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            epsilon = (float)e.NewValue;

            if (!initialized)
            {
                initialized = true;
                init();
            }

            Calc();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            generateGrid(20);

            canvas.Children.Clear();

            Draw(graph, Brushes.Turquoise);
            Draw(path, Brushes.GreenYellow);

            Calc();
        }
    }
}
