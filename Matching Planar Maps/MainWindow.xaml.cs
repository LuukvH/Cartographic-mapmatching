using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private Graph _graph;
        private Graph _path;

        private const float TOLERANCE = 0.0000001f;

        // Free space parameters
        private int steps = 50;
        private int _size = 100;

        public float epsilon { get; set; } = 25;

        private Boolean initialized = false;

        private List<Canvas>[] freeSpaceStrips;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void init()
        {
            _graph = new Graph(5);
            _graph.V[0] = new Vertex(20, 20);
            _graph.V[1] = new Vertex(80, 80);
            _graph.V[2] = new Vertex(110, 30);
            _graph.V[3] = new Vertex(150, 50);
            _graph.V[4] = new Vertex(240, 80);
            _graph.E[0].Add(1);
            _graph.E[1].Add(2);
            _graph.E[2].Add(3);
            _graph.E[2].Add(4);

            // The path
            _path = new Graph(4);
            _path.V[0] = new Vertex(20, 60);
            _path.V[1] = new Vertex(80, 30);
            _path.V[2] = new Vertex(120, 60);
            _path.V[3] = new Vertex(150, 60);
            _path.E[0].Add(1);
            _path.E[1].Add(2);
            _path.E[2].Add(3);
        }

        // Preprocessing
        public List<Interval>[] FD;
        private List<Interval>[] B;
        private List<Interval>[,] L;

        public void Calculation()
        {
            if (!initialized)
                init();

            Preprocessing();

            dynamicProgrammingStage();
        }

        public void Preprocessing()
        {
            FD = new List<Interval>[_graph.Size];
            B = new List<Interval>[_graph.Size];
            for (int i = 0; i < _graph.Size; i++)
            {
                FD[i] = new List<Interval>();
                B[i] = new List<Interval>();
                for (int n = 0; n < _path.E.Length; n++)
                {
                    // Continue if no outgoing edges
                    if (_path.E[n].Count <= 0)
                        continue;

                    Interval interval = GraphFunctions.CalculateInterval(_path.V[n], _path.V[_path.E[n][0]], _graph.V[i],
                        epsilon);

                    // Offset with index
                    interval.Start += n;
                    interval.End += n;
                    interval.PathIndex = n;
                    interval.GraphIndex = i;

                    if (!interval.Empty())
                        FD[i].Add(interval);

                    B[i].Add(interval);

                }
            }

            // Calculate L
            L = new List<Interval>[_graph.Size, _path.Size];
            for (int i = 0; i < _graph.Size; i++)
            {
                for (int p = 0; p < _path.Size; p++)
                {
                    L[i, p] = new List<Interval>();

                    for (int j = 0; j < _graph.E[i].Count; j++)
                    {
                        Interval interval = GraphFunctions.CalculateInterval(_graph.V[i], _graph.V[_graph.E[i][j]], _path.V[p],
                            epsilon);

                        L[i, p].Add(interval);
                    }
                }
            }

            LeftPointers();

            RightPointers();
        }

        public void LeftPointers()
        {
            for (int i = 0; i < _graph.Size; i++)
            {
                for (int j = 0; j < _graph.E[i].Count; j++)
                {
                    // For each white intervall in FDi calculate left pointers
                    foreach (Interval interval in FD[i])
                    {

                        float leftpointer = float.NaN;
                        int k = interval.PathIndex;
                        float max_ai = L[i, k][j].Start;

                        if (B[i][k].Start <= B[_graph.E[i][j]][k].End && !B[_graph.E[i][j]][k].Empty())
                        {
                            leftpointer = Math.Max(B[i][k].Start, B[_graph.E[i][j]][k].Start);
                        }
                        else
                        {
                            for (int n = k + 1; n < _path.Size - 1; n++)
                            {
                                if (L[i, n][j].End >= max_ai)
                                // Can move to this cell in a monetone path
                                {
                                    if (B[_graph.E[i][j]][n].Start > B[_graph.E[i][j]][n].End)
                                    {
                                        max_ai = Math.Max(L[i, n][j].Start, max_ai);
                                    }
                                    else
                                    {
                                        leftpointer = B[_graph.E[i][j]][n].Start;
                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        interval.LeftPointers.Add(leftpointer);
                    }
                }
            }
        }

        public void RightPointers()
        {
            for (int i = 0; i < _graph.Size; i++)
            {
                for (int j = 0; j < _graph.E[i].Count; j++)
                {
                    List<int> S = new List<int>();
                    int kp = 1;
                    float ai1 = 0f;
                    S.Add(0);

                    for (int k = 0; k < _path.Size - 1; k++)
                    {
                        kp = k;

                        while (kp < _path.Size - 1)
                        {
                            if (ai1 > L[i, k + 1][j].End || kp + 1 == _path.Size - 1 || L[i, k + 1][j].Empty())
                            {
                                // Maximal kp that fulfills (1)
                                int w = kp;

                                // Search white point to the left of kp + 1;
                                while (w > 0)
                                {
                                    if (!B[_graph.E[i][j]][w].Empty())
                                    {
                                        break;
                                    }
                                    w--;
                                }

                                if (k < w)
                                {
                                    B[i][k].RightPointers.Add(B[_graph.E[i][j]][w].End);
                                    //Console.WriteLine(B[_graph.E[i][j]][w].End);
                                    //rightPointers[k, j] = w + B[_graph.E[i][j]][w].End;
                                }
                                else if (k > w)
                                {
                                    B[i][k].RightPointers.Add(float.NaN);
                                    //Console.WriteLine(float.NaN);
                                    //rightPointers[k, j] = float.NaN;
                                }
                                else
                                {
                                    if (B[i][k].Start <= B[_graph.E[i][j]][k].End)
                                    {
                                        B[i][k].RightPointers.Add(B[_graph.E[i][j]][k].End);
                                        //Console.WriteLine(B[_graph.E[i][j]][k].End);
                                        //rightPointers[k, j] = k + B[_graph.E[i][j]][k].End;
                                    }
                                    else
                                    {
                                        B[i][k].RightPointers.Add(float.NaN);
                                        //Console.WriteLine(float.NaN);
                                        //rightPointers[k, j] = float.NaN;
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
                                while (S.Any() && L[i, S.Last()][j].Start > L[i, kp][j].Start)
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
            }
        }

        public void dynamicProgrammingStage()
        {
            //////////////////////////
            // Initialization phase //
            //////////////////////////
            List<Interval> Q = new List<Interval>();
            Interval[] C = new Interval[_graph.Size];
            Interval result = null;
            float x = 0;

            // Fill c
            for (int c = 0; c < C.Length; c++)
            {
                C[c] = new Interval();
                C[c].GraphIndex = c;
            }

            // Initialize Q
            for (int i = 0; i < _graph.Size; i++)
            {
                if (FD[i].Count > 0 && !FD[i][0].Empty() && FD[i][0].Start < TOLERANCE)
                {
                    Q.Add(FD[i][0]);
                    C[i].Start = FD[i][0].Start;
                    C[i].End = FD[i][0].End;
                }
            }

            // Init Q
            Console.WriteLine("-- Init Q --");
            print(Q);

            Console.WriteLine("-- Init Ci --");
            print(C);

            // If q is not empty continue otherwise no path exists
            while (Q.Any() && result == null)
            {
                // Sort q to priority
                Q = Q.OrderBy(q => q.Start).ToList();

                // Step 1 extract leftmost interval
                Interval I = Q.First(); // Get first interval
                Q.Remove(Q.First()); // Remove fist interval
                x = I.Start; // Advance x to l(I)
                Console.WriteLine("x: " + x);

                // Step 2 
                // Insert the next white interval of Ci which lies to right of I into Q
                for (int i = 0; i < FD[I.GraphIndex].Count; i++) // Search to the right for first white interval
                {
                    if (FD[I.GraphIndex][i].Start > I.Start)
                    {
                        Q.Add(FD[I.GraphIndex][i]);
                        Q = Q.OrderBy(q => q.Start).ToList(); // Should be log n
                        break;
                    }
                }

                // Step 3 / 4
                // Find all adjacent edges
                for (int j = 0; j< _graph.E[I.GraphIndex].Count; j++)
                {
                    Interval cj = C[_graph.E[I.GraphIndex][j]];
                    float lfttend = cj.Start;
                    if (!float.IsNaN(I.LeftPointers[j]))
                    {
                        if (I.LeftPointers[j] > cj.End || float.IsNaN(cj.End))
                        {
                            C[_graph.E[I.GraphIndex][j]] = new Interval(I.LeftPointers[j], I.RightPointers[j]);
                            C[_graph.E[I.GraphIndex][j]].GraphIndex = cj.GraphIndex;
                            C[_graph.E[I.GraphIndex][j]].PathPointer = I;
                            cj = C[_graph.E[I.GraphIndex][j]];
                        }
                    }

                    // If left point changed delete old interval of cj in q
                    if (float.IsNaN(lfttend) || Math.Abs(lfttend - cj.Start) > TOLERANCE)
                    {
                        // Remove old cj
                        foreach (Interval q in Q)
                        {
                            if (q.GraphIndex == cj.GraphIndex)
                            {
                                Q.Remove(q);
                                break;
                            }
                        }

                        // Insert new interval cj
                        for (int i = 0; i < FD[_graph.E[I.GraphIndex][j]].Count; i++)
                        {
                            if (FD[_graph.E[I.GraphIndex][j]][i].Start >= cj.Start && FD[_graph.E[I.GraphIndex][j]][i].Start <= cj.End)
                            {
                                Q.Add(FD[_graph.E[I.GraphIndex][j]][i]);
                                cj.PathPointer = I;
                                break;
                            }
                        }
                    }

                    // Set result if endpoint reached
                    //Console.WriteLine("Rightpointer values: " + rightPointers[cj.PathIndex, cj.GraphIndex]);
                    if (cj.End >= _path.Size - 1)
                    {
                        result = cj;
                        break;
                    }

                }

                // Print q
                Console.WriteLine("-- Show Q --");
                print(Q);

                // Print c
                Console.WriteLine("-- Show C --");
                print(C);
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
                    //resultPath.Add(DrawLine(_graph.V[result.GraphIndex], _graph.V[result.PathPointer.GraphIndex], Brushes.Blue));
                    Console.WriteLine("v" + result.GraphIndex);
                    result = result.PathPointer;
                }
            }
            else
            {
                Console.WriteLine("No feasable path exists");
            }

        }

        public void print(List<Interval> Q)
        {
            for (int i = 0; i < Q.Count; i++)
            {
                Interval interval = Q[i];
                Console.Write(String.Format("{0}: {1}, s: {2} e: {3}", i, interval.PathIndex, interval.GraphIndex, interval.Start, interval.End));
                

            }
        }

        public void print(Interval[] C)
        {
            for (int i = 0; i < C.Length; i++)
            {
                Interval interval = C[i];
                if (!interval.Empty())
                {
                    Console.Write(String.Format("{0}: l: {1:0.0000} r: {2:0.0000}, ", i, C[i].Start, C[i].End));

                    while (interval != null)
                    {
                        //resultPath.Add(DrawLine(_graph.V[result.GraphIndex], _graph.V[result.PathPointer.GraphIndex], Brushes.Blue));
                        Console.Write("v" + interval.GraphIndex);
                        interval = interval.PathPointer;
                    }
                    Console.WriteLine();
                }
            }
        }

        public List<Line> resultPath = new List<Line>();

        public void DrawQ(List<Interval> Q)
        {
            foreach (Interval q in Q)
            {
                Ellipse circle = CreateCircle(new Vertex(q.Start, 0), 20);
                circle.Fill = Brushes.Brown;
            }
        }

            


                /*
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
        */

                #region Draw

            public
            void ReDraw()
        {
            ReDraw(false);
        }

        public void ReDraw(bool drawFreespace)
        {

            canvas.Children.Clear();
            Draw(_graph, Brushes.Turquoise);
            Draw(_path, Brushes.GreenYellow);

            GenerateFreeSpaceStrips();

            if (drawFreespace)
            {
                DrawFreeSpaceDiagram();
            }

            DrawIntervals();

            DrawLeftPointers();

            DrawRightPointers();
        }

        public void Draw(Graph graph, Brush brush)
        {
            for (int i = 0; i < graph.Size; i++)
            {
                List<int> edges = graph.E[i];
                foreach (int j in edges)
                {
                    Line line = DrawLine(graph.V[i], graph.V[j], brush);
                    canvas.Children.Add(line);
                }
            }
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
            return line;
        }

        public Ellipse CreateCircle(Vertex v, float r)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Width = 2 * r;
            ellipse.Height = 2 * r;
            Canvas.SetLeft(ellipse, v.X - r);
            Canvas.SetTop(ellipse, v.Y - r);

            return ellipse;
        }

        public Ellipse DrawCircle(Vertex v, float r, Brush brush)
        {
            Ellipse ellipse = CreateCircle(v, r);
            ellipse.Stroke = brush;
            ellipse.StrokeThickness = 1;
            return ellipse;
        }

        public void GenerateFreeSpaceStrips()
        {
            // Generate free space strips
            freeSpaceStrips = new List<Canvas>[_graph.Size];
            freeSpaceStack.Children.Clear();

            for (int i = 0; i < _graph.Size; i++)
            {
                freeSpaceStrips[i] = new List<Canvas>();

                // For every outgoing edge
                for (int j = 0; j < _graph.E[i].Count; j++)
                {
                    Canvas freeSpaceStrip = new Canvas
                    {
                        Height = _size,
                        Background = Brushes.Gray,
                        LayoutTransform = new ScaleTransform(1, -1, .5, .5)
                    };

                    freeSpaceStack.Children.Insert(0, freeSpaceStrip);
                    freeSpaceStrips[i].Add(freeSpaceStrip);
                }
            }
        }

        public void DrawFreeSpaceDiagram()
        {
            for (int i = 0; i < _graph.Size; i++)
            {
                // For every outgoing edge
                for (int j = 0; j < _graph.E[i].Count; j++)
                {
                    Canvas freeSpaceStrip = freeSpaceStrips[i][j];

                    // For every edge in path
                    for (int n = 0; n < _path.Size; n++)
                    {
                        if (_path.E[n].Count <= 0)
                            continue;

                        for (int s = 0; s < steps; s++)
                        {
                            float loc = ((1f / steps) * s);
                            Vertex c = _path.V[n] + (_path.V[_path.E[n][0]] - _path.V[n]) * loc;

                            Interval interval = GraphFunctions.CalculateInterval(_graph.V[i], _graph.V[_graph.E[i][j]], c, epsilon);
                            if (!interval.Empty())
                            {
                                Line line = new Line()
                                {
                                    X1 = (loc + n) * _size,
                                    Y1 = interval.Start * _size,
                                    X2 = (loc + n) * _size,
                                    Y2 = interval.End * _size,
                                    Stroke = Brushes.White,
                                    StrokeThickness = 4
                                };
                                freeSpaceStrip.Children.Add(line);
                            }
                        }
                    }
                }
            }
        }

        public void DrawIntervals()
        {
            for (int i = 0; i < _graph.Size; i++)
            {
                // For every outgoing edge
                for (int j = 0; j < _graph.E[i].Count; j++)
                {
                    Canvas freeSpaceStrip = freeSpaceStrips[i][j];

                    // FD intervals
                    foreach (Interval interval in FD[i])
                    {
                        Line line = new Line()
                        {
                            X1 = interval.Start * _size,
                            Y1 = 0,
                            X2 = interval.End * _size,
                            Y2 = 0
                        };
                        line.Stroke = Brushes.Blue;
                        line.StrokeThickness = 2;
                        freeSpaceStrip.Children.Add(line);
                    }

                    foreach (Interval interval in FD[_graph.E[i][j]])
                    {
                        if (!interval.Empty())
                        {
                            Line line = new Line()
                            {
                                X1 = interval.Start * _size,
                                Y1 = _size,
                                X2 = interval.End * _size,
                                Y2 = _size
                            };
                            line.Stroke = Brushes.Blue;
                            line.StrokeThickness = 2;
                            freeSpaceStrip.Children.Add(line);
                        }
                    }

                    // L intervals
                    for (int p = 0; p < _path.Size; p++)
                    {
                        Interval interval = L[i, p][j];
                        if (!interval.Empty())
                        {
                            Line line = new Line()
                            {
                                X1 = _size * p,
                                Y1 = interval.Start * _size,
                                X2 = _size * p,
                                Y2 = interval.End * _size,
                            };
                            line.Stroke = Brushes.Blue;
                            line.StrokeThickness = 2;
                            freeSpaceStrip.Children.Add(line);
                        }
                    }
                }
            }
        }

        public void DrawLeftPointers()
        {
            for (int i = 0; i < _graph.Size; i++)
            {
                // For every outgoing edge
                for (int j = 0; j < _graph.E[i].Count; j++)
                {
                    Canvas freeSpaceStrip = freeSpaceStrips[i][j];

                    // FD intervals
                    foreach (Interval interval in FD[i])
                    {
                        if (float.IsNaN(interval.LeftPointers[j]))
                            continue;

                        Line line = new Line()
                        {
                            X1 = interval.Start * _size,
                            Y1 = 0,
                            X2 = interval.LeftPointers[j] * _size,
                            Y2 = _size
                        };
                        line.Stroke = Brushes.Green;
                        line.StrokeThickness = 1;
                        freeSpaceStrip.Children.Add(line);
                    }
                }
            }
        }

        public void DrawRightPointers()
        {
            for (int i = 0; i < _graph.Size; i++)
            {
                // For every outgoing edge
                for (int j = 0; j < _graph.E[i].Count; j++)
                {
                    Canvas freeSpaceStrip = freeSpaceStrips[i][j];

                    // FD intervals
                    foreach (Interval interval in FD[i])
                    {
                        if (float.IsNaN(interval.RightPointers[j]))
                            continue;

                        Line line = new Line()
                        {
                            X1 = interval.Start * _size,
                            Y1 = 0,
                            X2 = interval.RightPointers[j] * _size,
                            Y2 = _size
                        };
                        line.Stroke = Brushes.Red;
                        line.StrokeThickness = 1;
                        freeSpaceStrip.Children.Add(line);
                    }
                }
            }
        }

        #endregion

        #region Events

        private
            void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            Calculation();

            ReDraw(true);
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            epsilon = (float)e.NewValue;

            if (!initialized)
            {
                initialized = true;
                init();
            }

            Calculation();

            ReDraw(true);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        private void slider_DragCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            ReDraw(true);
        }

    }
}

