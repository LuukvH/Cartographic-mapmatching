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

        private bool _debug = false;

        private const float TOLERANCE = 0.0000001f;

        // Free space parameters
        private int steps = 50;
        private int _size = 50;

        public float epsilon { get; set; } = 25;

        private Boolean initialized = false;

        private List<FreeSpaceStrip>[] _freeSpaceStrips;

        private Interval _result = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void init()
        {
            _graph = new Graph(3);
            _graph.V[0] = new Vertex(20, 15);
            _graph.V[1] = new Vertex(120, 10);
            _graph.V[2] = new Vertex(20, 5);
            _graph.E[0].Add(1);
            _graph.E[1].Add(2);

            // The path
            _path = new Graph(5);
            _path.V[0] = new Vertex(40, 40);
            _path.V[1] = new Vertex(120, 60);
            _path.V[2] = new Vertex(40, 80);
            _path.V[3] = new Vertex(120, 100);
            _path.V[4] = new Vertex(140, 40);
            _path.E[0].Add(1);
            _path.E[1].Add(2);
            _path.E[2].Add(3);
            _path.E[3].Add(4);

            initialized = true;
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
                        float max_ai = 0; //L[i, k][j].Start;

                        if (B[i][k].Start <= B[_graph.E[i][j]][k].End && !B[_graph.E[i][j]][k].Empty())
                        {
                            leftpointer = Math.Max(B[i][k].Start, B[_graph.E[i][j]][k].Start);
                        }
                        else
                        {
                            for (int n = k + 1; n < _path.Size - 1; n++)
                            {
                                // Can move to this cell in a monetone path
                                if (!L[i, n][j].Empty() && L[i, n][j].End >= max_ai)
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
                            if (ai1 > L[i, kp + 1][j].End || kp + 1 == _path.Size - 1 || L[i, kp + 1][j].Empty())
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
                                }
                                else if (k > w)
                                {
                                    B[i][k].RightPointers.Add(float.NaN);
                                }
                                else
                                {
                                    if (!B[_graph.E[i][j]][k].Empty() && B[i][k].Start <= B[_graph.E[i][j]][k].End)
                                    {
                                        B[i][k].RightPointers.Add(B[_graph.E[i][j]][k].End);
                                    }
                                    else
                                    {
                                        B[i][k].RightPointers.Add(float.NaN);
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
                                    else
                                    {
                                        ai1 = L[i, S[0]][j].Start;
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
                        //k = kp;
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
            Range[] C = new Range[_graph.Size];
            Interval result = null;
            float x = 0;

            int case1 = 0;
            int case2 = 0;
            int case3 = 0;

            // Fill c
            for (int c = 0; c < C.Length; c++)
            {
                C[c] = new Range();
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
            if (_debug)
            {
                Console.WriteLine("-- Init Q --");
                print(Q);

                Console.WriteLine("-- Init Ci --");
                print(C);
            }

            // If q is not empty continue otherwise no path exists
            while (Q.Any() && result == null)
            {
                // Sort q to priority
                Q = Q.OrderBy(q => q.Start).ToList();

                // Step 1 extract leftmost interval
                Interval I = Q.First(); // Get first interval
                Q.Remove(Q.First()); // Remove fist interval
                x = I.Start; // Advance x to l(I)
                if (_debug)
                {
                    Console.WriteLine("x: " + x);
                }

                // Step 2 
                // Insert the next white interval of Ci which lies to right of I into Q
                for (int i = 0; i < FD[I.GraphIndex].Count; i++) // Search to the right for first white interval
                {
                    if (FD[I.GraphIndex][i].Start > I.Start && FD[I.GraphIndex][i].Start < C[I.GraphIndex].End)
                    {
                        Q.Add(FD[I.GraphIndex][i]);

                        if (FD[I.GraphIndex][i].PathPointer == null)
                        {
                            Console.WriteLine(i);
                        }

                        Q = Q.OrderBy(q => q.Start).ToList(); // Should be log n
                        break;
                    }
                }

                // Step 3 / 4
                // Find all adjacent edges
                for (int j = 0; j < _graph.E[I.GraphIndex].Count; j++)
                {
                    Range cj = C[_graph.E[I.GraphIndex][j]];
                    float lfttend = cj.Start;

                    if (float.IsNaN(I.LeftPointers[j]))
                        continue;
                    
                    if (I.LeftPointers[j] > cj.End || float.IsNaN(cj.End))
                    {
                        case1++;

                        C[_graph.E[I.GraphIndex][j]] = new Range(I.LeftPointers[j], I.RightPointers[j]);
                        C[_graph.E[I.GraphIndex][j]].GraphIndex = cj.GraphIndex;
                        cj = C[_graph.E[I.GraphIndex][j]];

                        foreach (Interval q in Q)
                        {
                            if (q.GraphIndex == cj.GraphIndex)
                            {
                                Q.Remove(q);
                                break;
                            }
                        }

                        // Insert new interval 
                        bool first = true;
                        for (int i = 0; i < FD[_graph.E[I.GraphIndex][j]].Count; i++)
                        {

                            if (FD[_graph.E[I.GraphIndex][j]][i].Start >= cj.Start && FD[_graph.E[I.GraphIndex][j]][i].Start <= cj.End)
                            {
                                if (first)
                                {
                                    Q.Add(FD[_graph.E[I.GraphIndex][j]][i]);
                                    first = false;
                                }

                                if (Math.Abs(FD[_graph.E[I.GraphIndex][j]][i].Start) > TOLERANCE)
                                {
                                    FD[_graph.E[I.GraphIndex][j]][i].PathPointer = FD[_graph.E[I.GraphIndex][j]][i].PathPointer ?? I;
                                }
                            }
                        }
                    }

                    // If left point changed delete old interval of cj in q
                    if (float.IsNaN(lfttend) || Math.Abs(lfttend - cj.Start) > TOLERANCE)
                    {
                        case2++;

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
                        bool first = true;
                        for (int i = 0; i < FD[_graph.E[I.GraphIndex][j]].Count; i++)
                        {

                            if (FD[_graph.E[I.GraphIndex][j]][i].Start >= cj.Start && FD[_graph.E[I.GraphIndex][j]][i].Start <= cj.End)
                            {
                                if (first)
                                {
                                    Q.Add(FD[_graph.E[I.GraphIndex][j]][i]);
                                    first = false;
                                }

                                if (Math.Abs(FD[_graph.E[I.GraphIndex][j]][i].Start) > TOLERANCE)
                                {
                                    FD[_graph.E[I.GraphIndex][j]][i].PathPointer = FD[_graph.E[I.GraphIndex][j]][i].PathPointer ?? I;
                                }
                            }
                        }
                    }

                    // If rightpoint has changed
                    if (I.RightPointers[j] > cj.End)
                    {
                        case3++;

                        cj.End = I.RightPointers[j];
                        //cj.PathPointer = I;

                        // Insert new interval cj

                        bool first = Q.All(c => c.GraphIndex != cj.GraphIndex);
                        for (int i = 0; i < FD[_graph.E[I.GraphIndex][j]].Count; i++)
                        {

                            if (FD[_graph.E[I.GraphIndex][j]][i].Start >= cj.Start && FD[_graph.E[I.GraphIndex][j]][i].Start <= cj.End)
                            {
                                if (first)
                                {
                                    Q.Add(FD[_graph.E[I.GraphIndex][j]][i]);
                                    first = false;
                                }

                                if (Math.Abs(FD[_graph.E[I.GraphIndex][j]][i].Start) > TOLERANCE)
                                {
                                    FD[_graph.E[I.GraphIndex][j]][i].PathPointer = FD[_graph.E[I.GraphIndex][j]][i].PathPointer ?? I;
                                }
                            }
                        }
                    }

                    // Set result if endpoint reached
                    // Console.WriteLine("Rightpointer values: " + rightPointers[cj.PathIndex, cj.GraphIndex]);
                    if (cj.End >= _path.Size - 1 - TOLERANCE)
                    {
                        result = Q.First(i => i.GraphIndex == cj.GraphIndex);
                    }
                    
                }

                // Print q
                if (_debug)
                {
                    Console.WriteLine("-- Show Q --");
                    print(Q);


                    // Print c
                    Console.WriteLine("-- Show C --");
                    print(C);
                }
            }

            if (result != null)
            {
                _result = result;
                Console.WriteLine("Feasable path exists");

            }
            else
            {
                _result = null;
                Console.WriteLine("No feasable path exists");
            }

            Console.WriteLine("Case1: " + case1);
            Console.WriteLine("Case2: " + case2);
            Console.WriteLine("Case3: " + case3);

        }

        public void print(List<Interval> Q)
        {
            for (int i = 0; i < Q.Count; i++)
            {
                Interval interval = Q[i];
                Console.Write(String.Format("{0}: p: {1}, g: {2}, s: {3} e: {4}, ",
                   i, interval.PathIndex, interval.GraphIndex, interval.Start, interval.End));

                for (int n = 0; n < interval.LeftPointers.Count; n++)
                {
                    Console.Write(String.Format("[{0}] l: {1}: r: {2}, ", _graph.E[interval.GraphIndex][n], interval.LeftPointers[n],
                        interval.RightPointers[n]));
                }
                Console.WriteLine();
            }
        }

        public void print(Range[] C)
        {
            for (int i = 0; i < C.Length; i++)
            {
                Range range = C[i];
                if (!range.Empty())
                {
                    if (!float.IsNaN(range.Start) || !float.IsNaN(range.End))
                    {
                        Console.Write(String.Format("{0}: l: {1:0.0000} r: {2:0.0000}, ", i, C[i].Start, C[i].End));
                        Console.WriteLine();
                    }
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



        #region Draw

        public void ReDraw()
        {
            ReDraw(false);
        }

        public void ReDraw(bool drawFreespace)
        {

            canvas.Children.Clear();
            Draw(_graph, Brushes.Turquoise);
            Draw(_path, Brushes.Black);

            GenerateFreeSpaceStrips();

            for (int i = 0; i < _graph.Size; i++)
            {
                // For every outgoing edge
                for (int j = 0; j < _graph.E[i].Count; j++)
                {
                    FreeSpaceStrip freeSpaceStrip = _freeSpaceStrips[i][j];

                    if (!freeSpaceStrip.active)
                        continue;

                    if (drawFreespace)
                        DrawFreeSpaceDiagram(freeSpaceStrip);

                    DrawIntervals(freeSpaceStrip);

                    DrawLeftPointers(freeSpaceStrip);

                    DrawRightPointers(freeSpaceStrip);
                }
            }

            DrawResult(_result);

            _c = _c ?? _path.V[0];
            canvas.Children.Add(DrawCircle(_c, epsilon, Brushes.Red));
        }

        public float Inv(float val)
        {
            return (val - _size) * -1;
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
            _freeSpaceStrips = new List<FreeSpaceStrip>[_graph.Size];
            freeSpaceStack.Children.Clear();

            for (int i = 0; i < _graph.Size; i++)
            {
                _freeSpaceStrips[i] = new List<FreeSpaceStrip>(_graph.Size * 4);

                // For every outgoing edge
                for (int j = 0; j < _graph.E[i].Count; j++)
                {
                    FreeSpaceStrip freeSpaceStrip = new FreeSpaceStrip(i, _graph.E[i][j], _size);
                    freeSpaceStrip.MouseEnter += freespacestrip_MouseEnter;
                    freeSpaceStrip.MouseLeftButtonDown += freespacestrip_Click;

                    if (FD[i].Count > 0 || FD[_graph.E[i][j]].Count > 0)
                    {
                        foreach (Interval interval in FD[i])
                        {
                            if (!float.IsNaN(interval.LeftPointers[j]) ||
                                !float.IsNaN(interval.RightPointers[j]))
                            {
                                freeSpaceStrip.wbmp = BitmapFactory.New(_size * _path.Size, _size);
                                freeSpaceStrip.wbmp.Clear(Colors.Gray);
                                freeSpaceStrip.imgControl.Source = freeSpaceStrip.wbmp;
                                freeSpaceStack.Children.Insert(0, freeSpaceStrip);
                                freeSpaceStrip.active = true;
                                break;
                            }
                        }
                    }

                    _freeSpaceStrips[i].Add(freeSpaceStrip);
                }
            }
        }

        public void DrawFreeSpaceDiagram(FreeSpaceStrip freeSpaceStrip)
        {
            int i = freeSpaceStrip.I;
            int j = _graph.E[i].IndexOf(freeSpaceStrip.J);

            freeSpaceStrip.Canvas.Width = _size * _path.Size;
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
                        int X1 = Convert.ToInt32((loc + n) * _size);
                        int Y1 = Convert.ToInt32(Inv(interval.Start * _size));
                        int X2 = Convert.ToInt32((loc + n) * _size);
                        int Y2 = Convert.ToInt32(Inv(interval.End * _size));

                        freeSpaceStrip.wbmp.DrawLine(X1, Y1, X2, Y2, Colors.White);
                        //Line line = new Line()
                        //{
                        //    X1 = (loc + n) * _size,
                        //    Y1 = interval.Start * _size,
                        //    X2 = (loc + n) * _size,
                        //    Y2 = interval.End * _size,
                        //    Stroke = Brushes.White,
                        //    StrokeThickness = 4
                        //};
                        //freeSpaceStrip.Canvas.Children.Add(line);
                    }
                }
            }
        }

        public void DrawIntervals(FreeSpaceStrip freeSpaceStrip)
        {
            int i = freeSpaceStrip.I;
            int j = _graph.E[i].IndexOf(freeSpaceStrip.J);

            // FD intervals
            foreach (Interval interval in FD[i])
            {
                int X1 = Convert.ToInt32(interval.Start * _size);
                int Y1 = Convert.ToInt32(_size);
                int X2 = Convert.ToInt32(interval.End * _size);
                int Y2 = Convert.ToInt32(_size);
                freeSpaceStrip.wbmp.DrawLine(X1, Y1, X2, Y2, Colors.Blue);
            }

            foreach (Interval interval in FD[_graph.E[i][j]])
            {
                if (!interval.Empty())
                {
                    int X1 = Convert.ToInt32(interval.Start * _size);
                    int Y1 = Convert.ToInt32(0);
                    int X2 = Convert.ToInt32(interval.End * _size);
                    int Y2 = Convert.ToInt32(0);
                    freeSpaceStrip.wbmp.DrawLine(X1, Y1, X2, Y2, Colors.Blue);
                }
            }

            // L intervals
            for (int p = 0; p < _path.Size; p++)
            {
                Interval interval = L[i, p][j];
                if (!interval.Empty())
                {
                    int X1 = Convert.ToInt32(_size * p);
                    int Y1 = Convert.ToInt32(Inv(interval.Start * _size));
                    int X2 = Convert.ToInt32(_size * p);
                    int Y2 = Convert.ToInt32(Inv(interval.End * _size));
                    freeSpaceStrip.wbmp.DrawLine(X1, Y1, X2, Y2, Colors.Blue);
                }
            }
        }

        public void DrawLeftPointers(FreeSpaceStrip freeSpaceStrip)
        {
            int i = freeSpaceStrip.I;
            int j = _graph.E[i].IndexOf(freeSpaceStrip.J);

            // FD intervals
            foreach (Interval interval in FD[i])
            {
                if (float.IsNaN(interval.LeftPointers[j]))
                    continue;

                int X1 = Convert.ToInt32(interval.Start * _size);
                int Y1 = Convert.ToInt32(_size);
                int X2 = Convert.ToInt32(interval.LeftPointers[j] * _size);
                int Y2 = Convert.ToInt32(0);
                freeSpaceStrip.wbmp.DrawLine(X1, Y1, X2, Y2, Colors.Green);
            }
        }

        public void DrawRightPointers(FreeSpaceStrip freeSpaceStrip)
        {
            int i = freeSpaceStrip.I;
            int j = _graph.E[i].IndexOf(freeSpaceStrip.J);

            // FD intervals
            foreach (Interval interval in FD[i])
            {
                if (float.IsNaN(interval.RightPointers[j]))
                    continue;

                int X1 = Convert.ToInt32(interval.Start * _size);
                int Y1 = Convert.ToInt32(_size);
                int X2 = Convert.ToInt32(interval.RightPointers[j] * _size);
                int Y2 = Convert.ToInt32(0);
                freeSpaceStrip.wbmp.DrawLine(X1, Y1, X2, Y2, Colors.Red);
            }
        }

        public void DrawResult(Interval result)
        {
            while (result != null && result.PathPointer != null)
            {
                Line line = DrawLine(_graph.V[result.GraphIndex], _graph.V[result.PathPointer.GraphIndex], Brushes.Blue);
                resultPath.Add(line);
                canvas.Children.Add(line);
                result = result.PathPointer;
            }
        }

        #endregion

        #region Events

        private Vertex _c = null;
        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            Point p = Mouse.GetPosition(canvas);
            _c = new Vertex((float)p.X, (float)p.Y);

            _debug = false;
            Calculation();
            _debug = false;

            ReDraw(true);
        }

        private Line _edge_highlighting = null;
        private void freespacestrip_MouseEnter(object sender, RoutedEventArgs e)
        {
            FreeSpaceStrip fss = ((FreeSpaceStrip)sender);

            try
            {
                canvas.Children.Remove(_edge_highlighting);
                _edge_highlighting = DrawLine(_graph.V[fss.I], _graph.V[fss.J], Brushes.Red);
                canvas.Children.Add(_edge_highlighting);
            }
            catch
            {
                // ignored
            }
        }

        private void freespacestrip_Click(object sender, RoutedEventArgs e)
        {
            FreeSpaceStrip fss = ((FreeSpaceStrip)sender);

            Graph graph = new Graph(2);
            graph.V[0] = _graph.V[fss.I];
            graph.V[1] = _graph.V[fss.J];
            graph.E[0].Add(1);
            //graph.E[1].Add(0);

            _graph = graph;

            Calculation();


            //ReDraw(true);
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            _debug = false;

            epsilon = (float)e.NewValue;
            try
            {
                lblEpsilon.Content = epsilon.ToString();
            }
            catch
            {
                // ignored
            }

            /*
            if (!initialized)
            {
                initialized = true;
                init();
            }

            Calculation();

            ReDraw(false);
            */
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (!initialized)
                init();

            _graph = new GridGraph(30, 50, 10f);

            canvas.Children.Clear();
            Draw(_graph, Brushes.Turquoise);
            Draw(_path, Brushes.Black);
        }
        #endregion

        private void slider_DragCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            Calculation();
            ReDraw(true);
        }

        private void Result_Click(object sender, RoutedEventArgs e)
        {
            if (_result != null)
            {
                freeSpaceStack.Children.Clear();
                Interval result = _result;
                while (result != null && result.PathPointer != null)
                {
                    FreeSpaceStrip freeSpaceStrip = new FreeSpaceStrip(result.PathPointer.GraphIndex, result.GraphIndex, _size);
                    freeSpaceStrip.MouseEnter += freespacestrip_MouseEnter;
                    freeSpaceStrip.MouseLeftButtonDown += freespacestrip_Click;

                    freeSpaceStrip.wbmp = BitmapFactory.New(_size * _path.Size, _size);
                    freeSpaceStrip.wbmp.Clear(Colors.Gray);
                    freeSpaceStrip.imgControl.Source = freeSpaceStrip.wbmp;

                    DrawFreeSpaceDiagram(freeSpaceStrip);
                    DrawIntervals(freeSpaceStrip);
                    DrawLeftPointers(freeSpaceStrip);
                    DrawRightPointers(freeSpaceStrip);
                    freeSpaceStack.Children.Add(freeSpaceStrip);

                    result = result.PathPointer;
                }
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (!initialized)
                init();

            FileReader fileReader = new FileReader();
            _path = fileReader.test("./Samples/Vietnam_coords.ipe");

            canvas.Children.Clear();
            Draw(_graph, Brushes.Turquoise);
            Draw(_path, Brushes.Black);
        }
    }
}


