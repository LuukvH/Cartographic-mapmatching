using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace Matching_Planar_Maps
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GridGraph _graph;
        private Graph _path;
        
        private string currentFile = ""; 

        private bool _debug = false;

        private bool _allowSameEdge = true;

        private const float TOLERANCE = 0.0000001f;

        private Polygon inputPolygon;
        private Polygon outputPolygon;
        private Polygon xorPolygon;

        // Free space parameters
        private int steps = 80;
        private int _size = 80;

        public float maxDistance = 1;
        //public float epsilon { get; set; } = 500;
        public float epsilon { get; set; } = 10;
        private Boolean initialized = false;

        private List<FreeSpaceStrip>[] _freeSpaceStrips;

        private Interval _result = null;

        private string outputfolder = "Experiment1/";

        private int reusedEdges = 0;
        private int reusedVertices = 0;

        public MainWindow()
        {
            InitializeComponent();

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            //dispatcherTimer.Start();
        }

        public void init()
        {
            //_graph = new GridGraph(8, 8, 80f);
            _graph = new GridGraph(51, 51, 1f);

        }

        Random random = new Random();
        public float randomFloat(float maxValue)
        {
            float randomFloat = (float)random.NextDouble();
            return randomFloat * maxValue;
        }

        // Preprocessing
        public List<Interval>[] FD;
        private List<Interval>[] B;
        private List<Interval>[,] L;

        public void Calculation()
        {
            if (!initialized)
                init();

            epsilon = 6.838623f;
            Calculate();

            CreateResultGraph(_result);

            //PostProcessing();

            //DrawResult(_result);

            //ReDraw();

            //return;

            int maxSteps = 20;
            int steps = 0;
            float minValue = 0;
            //float maxValue = 200;
            float maxValue = 1;
            epsilon = 1;

            // Determine max and minvalue
            Console.WriteLine("Epsilon: {0}", epsilon);
            while (Calculate() == null)
            {
                minValue = maxValue;
                maxValue *= 2;
                epsilon = maxValue;
                Console.WriteLine("Epsilon: {0}", epsilon);
            }

            // Find epsilon between min and max
            while (maxValue - minValue > 0.0001)
            {
                epsilon = (minValue + maxValue) / 2;
                Console.WriteLine("Epsilon: {0}", epsilon);
                if (Calculate() != null)
                {
                    maxValue = epsilon;
                }
                else
                {
                    minValue = epsilon;
                }
            }

            epsilon = maxValue;
            Calculate();

            CreateResultGraph(_result);

        }

        public Interval Calculate()
        {
            Preprocessing();

            return dynamicProgrammingStage();
        }

        public void Preprocessing()
        {
            if (_path == null || _path.Size <= 1)
                return;

            var watch = new System.Diagnostics.Stopwatch();

            Console.Write("Allocating Memory");
            watch.Reset();
            watch.Start();
            FD = new List<Interval>[_graph.Size];
            B = new List<Interval>[_graph.Size];
            watch.Stop();
            Console.WriteLine(" ({0} ms)", watch.ElapsedMilliseconds);

            Console.Write("Calculating B");
            watch.Reset();
            watch.Start();
            for (int i = 0; i < _graph.Size; i++)
            {
                FD[i] = new List<Interval>();
                B[i] = new List<Interval>();
                for (int n = 0; n < _path.E.Length; n++)
                {
                    // Continue if no outgoing edges
                    if (_path.E[n].Count <= 0)
                        continue;

                    Interval interval;
                    if (n == 0 || n == _path.E.Length - 2)
                    {
                        interval = GraphFunctions.CalculateInterval(_path.V[n], _path.V[_path.E[n][0]],
                            _graph.V[i],
                            (float)(Math.Sqrt(2) / 2) - TOLERANCE);
                    }
                    else
                    {
                        interval = GraphFunctions.CalculateInterval(_path.V[n], _path.V[_path.E[n][0]],
                            _graph.V[i],
                            epsilon);
                    }

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
            watch.Stop();
            Console.WriteLine(" ({0} ms)", watch.ElapsedMilliseconds);

            // Calculate L
            Console.Write("Calculating L");
            watch.Reset();
            watch.Start();

            L = new List<Interval>[_graph.Size, _path.Size];
            for (int i = 0; i < _graph.Size; i++)
            {
                for (int p = 0; p < _path.Size; p++)
                {
                    L[i, p] = new List<Interval>(4);

                    for (int j = 0; j < _graph.E[i].Count; j++)
                    {
                        // Validate if cell is reachable otherwise set interval empty
                        Interval interval = new Interval(1, 0);
                        if (p <= 0 || !L[i, p - 1][j].Empty() || !B[i][p - 1].Empty())
                        {
                            if (p == 0 || p == _path.E.Length - 2)
                            {
                                interval = GraphFunctions.CalculateInterval(_graph.V[i], _graph.V[_graph.E[i][j]],
                                   _path.V[p], (float)(Math.Sqrt(2) / 2) - TOLERANCE);
                            }
                            else
                            {
                                interval = GraphFunctions.CalculateInterval(_graph.V[i], _graph.V[_graph.E[i][j]],
                                    _path.V[p], epsilon);
                            }
                        }

                        L[i, p].Add(interval);
                    }
                }
            }
            watch.Stop();
            Console.WriteLine(" ({0} ms)", watch.ElapsedMilliseconds);

            Console.Write("Calculating LeftPointers");
            watch.Reset();
            watch.Start();
            LeftPointers();
            watch.Stop();
            Console.WriteLine(" ({0} ms)", watch.ElapsedMilliseconds);

            Console.Write("Calculating RightPointers");
            watch.Reset();
            watch.Start();
            RightPointers();
            watch.Stop();
            Console.WriteLine(" ({0} ms)", watch.ElapsedMilliseconds);
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
                if (!FD[i].Any())
                    continue;

                for (int j = 0; j < _graph.E[i].Count; j++)
                {
                    //if (!FD[_graph.E[i][j]].Any())
                    //{
                    //    for (int n = 0; n < FD[i].Count; n++)
                    //    {
                    //        FD[i][n].RightPointers.Add(float.NaN);
                    //    }
                    //    break;
                    //}

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
                                //k = kp;
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

        public Interval dynamicProgrammingStage()
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

                    // Disallow going back on same edge
                    if (I.PathPointer != null && _graph.E[I.GraphIndex][j] == I.PathPointer.GraphIndex && !_allowSameEdge)
                        continue;

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
                        try
                        {
                            result = Q.First(i => i.GraphIndex == cj.GraphIndex);
                        }
                        catch
                        {
                        }
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


            return _result;
        }

        public float CalculateFrechetDistance()
        {
            Interval[,] B = new Interval[_path.Size + 1, _graph.Size + 1];
            Interval[,] BR = new Interval[_path.Size + 1, _graph.Size + 1];
            float[,] LR = new float[_path.Size + 1, _graph.Size + 1];

            for (int i = 0; i < _path.E.Length; i++)
            {
                for (int j = 0; j < _graph.Size; j++)
                {
                    // Continue if no outgoing edges
                    if (_path.E[i].Count <= 0)
                        continue;

                    Interval interval = GraphFunctions.CalculateInterval(_path.V[i], _path.V[_path.E[i][0]], _graph.V[j],
                        epsilon);

                    // Offset with index
                    interval.Start += i;
                    interval.End += i;
                    interval.PathIndex = i;
                    interval.GraphIndex = i;

                    B[i, j] = interval;
                }
            }

            // Horizontal (path)
            for (int i = 0; i < _path.Size; i++)
            {
                BR[i, 0] = new Interval();
            }

            // Vertical (graph)
            for (int j = 0; j < _graph.Size; j++)
            {

            }

            for (int i = 1; i < _path.Size; i++)
            {
                for (int j = 1; j < _graph.Size; j++)
                {
                    BR[i, j] = new Interval();
                    BR[i, j].Start = Math.Max(BR[i - 1, j - 1].Start, B[i, j].Start);
                }
            }
            return 0.5f;
        }


        public Graph CreateResultGraph(Interval result)
        {
            if (result == null)
                return null;

            List<Vertex> vertices = new List<Vertex>();
            while (result != null)
            {
                vertices.Add(_graph.V[result.GraphIndex]);
                result = result.PathPointer;
            }

            // Add all vertices to graph
            vertices.Reverse();
            Graph graph = new Graph(vertices.Count);
            graph.V = vertices.ToArray();

            // Add all edges
            for (int i = 0; i < graph.Size - 1; i++)
            {
                graph.E[i].Add(i + 1);
            }

            return graph;
        }

        public void DetectSameEdges()
        {
            PostProcessing();
        }

        private List<int> path;

        public void PostProcessing()
        {
            // Build graph based on grid
            Graph resultGraph = new Graph(_graph.Size);
            resultGraph.V = _graph.V;

            path = new List<int>();
            Interval result = _result;
            while (result != null && result.PathPointer != null)
            {
                path.Add(result.PathPointer.GraphIndex);
                resultGraph.E[result.GraphIndex].Add(result.PathPointer.GraphIndex);
                result = result.PathPointer;
            }

            //Draw(OutputCanvas, resultGraph, Brushes.Blue);

            // Reset counts
            reusedEdges = 0;
            reusedVertices = 0;

            // Detect reuse of edges
            for (int i = 1; i < path.Count; i++)
            {
                for (int n = i + 1; n < path.Count; n++)
                {
                    if ((path[i] == path[n] && path[i - 1] == path[n - 1]) ||
                        (path[i - 1] == path[n] && path[i] == path[n - 1]))
                    {
                        // Indicate reuse of edges
                        DetectCanvas.Children.Add(DrawLine(resultGraph.V[path[i - 1]], resultGraph.V[path[i]], Brushes.Red));

                        reusedEdges++;

                        //int v1 = path[i];
                        //int v2 = path[i - 1];

                        //int x = (_graph as GridGraph).GridX(path[i]);
                        //int y = (_graph as GridGraph).GridY(path[i]) + 1;
                        //int index = (_graph as GridGraph).GridIndex(x, y);

                        //int x2 = (_graph as GridGraph).GridX(path[i - 1]);
                        //int y2 = (_graph as GridGraph).GridY(path[i - 1]) + 1;
                        //int index2 = (_graph as GridGraph).GridIndex(x2, y2);

                        //DetectCanvas.Children.Add(DrawLine(resultGraph.V[index], resultGraph.V[index2], Brushes.Purple));

                        //path[i] = index;
                        //path[i - 1] = index2;

                        //if (v1 != path[i + 1])
                        //    path.Insert(i + 1, v1);

                        //if (path[i - 1] == path[i - 2])
                        //{
                        //    path.RemoveAt(i - 1);
                        //}
                    }
                }
            }

            // Remove direct edge return
            //for (int i = 1; i < path.Count - 1; i++)
            //{
            //    // Remove direct edge return
            //    if (path[i - 1] == path[i + 1])
            //    {
            //        path.RemoveAt(i);
            //        i--;
            //    }

            //    // Remove double vertices
            //    if (path[i] == path[i - 1])
            //    {
            //        path.RemoveAt(i);
            //        i--;
            //    }
            //}

            //for (int i = 1; i < path.Count; i++)
            //{
            //    DetectCanvas.Children.Add(DrawLine(resultGraph.V[path[i - 1]], resultGraph.V[path[i]], Brushes.Yellow));
            //}

            /*
                // Detect reuse of edge
                for (int v = 0; v < resultGraph.Size; v++)
                {
                    foreach (int edge in resultGraph.E[v])
                    {
                        // Is a duplicate edge
                        if (resultGraph.E[edge].Contains(v))
                        {
                            canvas.Children.Add(DrawLine(resultGraph.V[v], resultGraph.V[edge], Brushes.Red));

                            // When horizontal then push up
                            int x = (_graph as GridGraph).GridX(edge);
                            int y = (_graph as GridGraph).GridY(edge) - 1;
                            int index = (_graph as GridGraph).GridIndex(x, y);

                            int x2 = (_graph as GridGraph).GridX(v);
                            int y2 = (_graph as GridGraph).GridY(v) - 1;
                            int index2 = (_graph as GridGraph).GridIndex(x2, y2);

                            canvas.Children.Add(DrawLine(resultGraph.V[index], resultGraph.V[index2], Brushes.Purple));
                        }
                    }
                }
                */


            // Detect reuse of vertices
            var duplicates = path.GroupBy(i => i)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToList();
            foreach (int v in duplicates)
            {
                DetectCanvas.Children.Add(DrawCircle(resultGraph.V[v], 6.0f, 2, Brushes.Red));
                reusedVertices++;
            }
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


        private int currentindex = 0;
        private Line lineanimater = null;
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (lineanimater != null)
            {
                OutputCanvas.Children.Remove(lineanimater);
            }

            if (path != null && path.Count > 0)
            {
                currentindex = (currentindex + 1) % (path.Count - 1);
                int nextindex = (currentindex + 1) % (path.Count - 1);
                lineanimater = DrawLine(_graph.V[path[currentindex]], _graph.V[path[nextindex]],
                    Brushes.Red);
                OutputCanvas.Children.Add(lineanimater);
            }
        }

        #region Draw

        public void ReDraw()
        {
            ReDraw(false);
        }

        public void ReDraw(bool drawFreespace)
        {

            GridCanvas.Children.Clear();
            Draw(GridCanvas, _graph, Brushes.LightGray);
            Draw(InputCanvas, _path, Brushes.Black);

            //GenerateFreeSpaceStrips();

            for (int i = 0; i < _graph.Size; i++)
            {
                /*
                // For every outgoing edge
                for (int j = 0; j < _graph.E[i].Count; j++)
                {
                    FreeSpaceStrip freeSpaceStrip = _freeSpaceStrips[i][j];
                    
                    if (!freeSpaceStrip.active)
                        continue;

                    //if (drawFreespace)
                        DrawFreeSpaceDiagram(freeSpaceStrip);

                    DrawIntervals(freeSpaceStrip);

                    DrawLeftPointers(freeSpaceStrip);

                    DrawRightPointers(freeSpaceStrip);
                    
                }
                */
            }

            DrawResult(_result);


        }

        public float Inv(float val)
        {
            return (val - _size) * -1;
        }

        public void Draw(Canvas canvas, Graph graph, Brush brush)
        {
            if (graph == null)
                return;

            for (int i = 0; i < graph.Size; i++)
            {
                List<int> edges = graph.E[i];
                foreach (int j in edges)
                {
                    Line line = new Line()
                    {
                        X1 = graph.V[i].X * 10,
                        Y1 = graph.V[i].Y * 10,
                        X2 = graph.V[j].X * 10,
                        Y2 = graph.V[j].Y * 10,
                        Stroke = brush,
                        StrokeThickness = 1
                    };
                    canvas.Children.Add(line);
                }
            }
        }

        public Line DrawLine(Vertex v1, Vertex v2, Brush brush)
        {
            Line line = new Line()
            {
                X1 = v1.X * 10,
                Y1 = v1.Y * 10,
                X2 = v2.X * 10,
                Y2 = v2.Y * 10
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

        public Ellipse DrawCircle(Vertex v, float r, double StrokeThickness, Brush brush)
        {
            Ellipse ellipse = CreateCircle(new Vertex(v.X * 10, v.Y * 10), r);
            ellipse.Stroke = brush;
            ellipse.StrokeThickness = StrokeThickness;
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
                                freeSpaceStrip.wbmp = BitmapFactory.New(_size * (_path.Size - 1), _size);
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
            for (int n = 0; n < _path.Size - 1; n++)
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
                OutputCanvas.Children.Add(line);
                result = result.PathPointer;
            }

            Graph resultGraph = CreateResultGraph(_result);
            outputPolygon = new Polygon();
            Polygon zoomedOutputPolygon = new Polygon();
            foreach (Vertex v in resultGraph.V)
            {
                outputPolygon.Points.Add(new Point(v.X, v.Y));
                zoomedOutputPolygon.Points.Add(new Point(v.X * 10, v.Y * 10));
            }
            zoomedOutputPolygon.Fill = Brushes.Blue;
            zoomedOutputPolygon.Opacity = 0.25;
            OutputPolygonCanvas.Children.Add(zoomedOutputPolygon);

           inputPolygon.Stroke = Brushes.Black;
            outputPolygon.Fill = Brushes.Blue;
            RenderCanvas.Children.Clear();
            RenderCanvas.Children.Add(outputPolygon);
            RenderCanvas.Children.Add(inputPolygon);

        }

        #endregion

        #region Events

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = Mouse.GetPosition(GridCanvas);

            Vertex v = new Vertex((float)p.X / 10, (float)p.Y / 10);

            if (_path == null)
                _path = new Path(0);

            List<Vertex> V = _path.V.ToList();
            V.Add(v);

            _path = new Path(V.Count);
            _path.V = V.ToArray();

            InputCanvas.Children.Clear();
            Draw(InputCanvas, _path, Brushes.Black);

            //Calculation();

            //if (_result != null)
            //{
            //    lblEpsilon.Content = epsilon;
            //}
            //else
            //{
            //    lblEpsilon.Content = '-';
            //}

            //ReDraw(true);
        }

        private void canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_path.V.Length > 0)
            {
                List<Vertex> V = _path.V.ToList();
                V.RemoveAt(V.Count - 1);
                _path = new Path(V.Count);
                _path.V = V.ToArray();

                //Calculation();
            }

            //if (_result != null)
            //{
            //    lblEpsilon.Content = epsilon;
            //}
            //else
            //{
            //    lblEpsilon.Content = '-';
            //}

            //ReDraw(true);

            InputCanvas.Children.Clear();
            Draw(InputCanvas, _path, Brushes.Black);
        }

        private Line _edge_highlighting = null;
        private void freespacestrip_MouseEnter(object sender, RoutedEventArgs e)
        {
            FreeSpaceStrip fss = ((FreeSpaceStrip)sender);

            try
            {
                OutputCanvas.Children.Remove(_edge_highlighting);
                _edge_highlighting = DrawLine(_graph.V[fss.I], _graph.V[fss.J], Brushes.Red);
                OutputCanvas.Children.Add(_edge_highlighting);
            }
            catch
            {
                // ignored
            }
        }

        private void freespacestrip_Click(object sender, RoutedEventArgs e)
        {
            //FreeSpaceStrip fss = ((FreeSpaceStrip)sender);

            //Graph graph = new Graph(2);
            //graph.V[0] = _graph.V[fss.I];
            //graph.V[1] = _graph.V[fss.J];
            //graph.E[0].Add(1);
            ////graph.E[1].Add(0);

            //_graph = graph;

            //Calculation();


            //ReDraw(true);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (!initialized)
                init();

            //_graph = new GridGraph(30, 50, 10f);

            Draw(InputCanvas, _path, Brushes.Black);

            Calculation();

            ReDraw();

            DetectSameEdges();

        }
        #endregion

        private CombinedGeometry geom = null;
        private System.Windows.Shapes.Path viewpath = null;
        private void Result_Click(object sender, RoutedEventArgs e)
        {


            if (_path == null || _path.Size <= 0)
                return;



            // Only input
            SaveAsImage(currentFile);

            //Calculation();

            /*
            canvas.Children.Clear();
            Draw(_path, Brushes.Black);
            DrawResult(_result);

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

                    DrawFreeSpaceDiagram(freeSpaceStrip);8
                    DrawIntervals(freeSpaceStrip);
                    DrawLeftPointers(freeSpaceStrip);
                    DrawRightPointers(freeSpaceStrip);
                    freeSpaceStack.Children.Add(freeSpaceStrip);

                    result = result.PathPointer;
                }
            }

            //if (_result != null)
            //{
            //    _graph = CreateResultGraph(_result);
            //}
            */




            //Console.WriteLine("Frechet distance: {0}", CalculateFrechetDistance());



        }

        private void SaveAsImage(string filename)
        {
            // Render output
            RenderTargetBitmap gridRTB = new RenderTargetBitmap((int)GridCanvas.RenderSize.Width, (int)GridCanvas.RenderSize.Height, 96d, 96d, PixelFormats.Pbgra32);
            RenderTargetBitmap inputRTB = new RenderTargetBitmap((int)InputCanvas.RenderSize.Width, (int)InputCanvas.RenderSize.Height, 96d, 96d, PixelFormats.Pbgra32);
            RenderTargetBitmap outputPolygonRTB = new RenderTargetBitmap((int)OutputPolygonCanvas.RenderSize.Width, (int)OutputPolygonCanvas.RenderSize.Height, 96d, 96d, PixelFormats.Pbgra32);
            RenderTargetBitmap outputRTB = new RenderTargetBitmap((int)OutputCanvas.RenderSize.Width, (int)OutputCanvas.RenderSize.Height, 96d, 96d, PixelFormats.Pbgra32);
            RenderTargetBitmap detectRTB = new RenderTargetBitmap((int)DetectCanvas.RenderSize.Width, (int)DetectCanvas.RenderSize.Height, 96d, 96d, PixelFormats.Pbgra32);

            gridRTB.Render(GridCanvas);
            inputRTB.Render(InputCanvas);
            outputPolygonRTB.Render(OutputPolygonCanvas);
            outputRTB.Render(OutputCanvas);
            detectRTB.Render(DetectCanvas);

            var gridCrop = new CroppedBitmap(gridRTB, new Int32Rect(0, 0, 500, 500));
            var inputCrop = new CroppedBitmap(inputRTB, new Int32Rect(0, 0, 500, 500));
            var outputPolygonCrop = new CroppedBitmap(outputPolygonRTB, new Int32Rect(0, 0, 500, 500));
            var outputCrop = new CroppedBitmap(outputRTB, new Int32Rect(0, 0, 500, 500));
            var detectCrop = new CroppedBitmap(detectRTB, new Int32Rect(0, 0, 500, 500));

            //Combine the images here
            var id1 = new ImageDrawing(gridCrop, new Rect(0, 0, gridCrop.Width, gridCrop.Height));
            var id2 = new ImageDrawing(inputCrop, new Rect(0, 0, inputCrop.Width, inputCrop.Height));
            var id5 = new ImageDrawing(outputPolygonCrop, new Rect(0, 0, outputPolygonCrop.Width, outputPolygonCrop.Height));
            var id3 = new ImageDrawing(outputCrop, new Rect(0, 0, outputCrop.Width, outputCrop.Height));
            var id4 = new ImageDrawing(detectCrop, new Rect(0, 0, detectCrop.Width, detectCrop.Height));

            var dg = new DrawingGroup();
            dg.Children.Add(id1);
            dg.Children.Add(id2);
            dg.Children.Add(id5);
            dg.Children.Add(id3);
            dg.Children.Add(id4);

            var combinedImg = new RenderTargetBitmap((int)gridCrop.Width, (int)gridCrop.Height, 96, 96, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawDrawing(dg);
            }
            combinedImg.Render(dv);

            BitmapEncoder pngEncoder;

            pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(inputCrop));
            using (var fs = System.IO.File.OpenWrite(string.Format("{0}{1}-input.png", outputfolder, filename)))
            {
                pngEncoder.Save(fs);
            }

            pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(outputPolygonCrop));
            using (var fs = System.IO.File.OpenWrite(string.Format("{0}{1}-outputPolygon-{2}-{3}-{4}.png", outputfolder, filename, epsilon, reusedEdges, reusedVertices)))
            {
                pngEncoder.Save(fs);
            }

            pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(outputCrop));
            using (var fs = System.IO.File.OpenWrite(string.Format("{0}{1}-output-{2}-{3}-{4}.png", outputfolder, filename, epsilon, reusedEdges, reusedVertices)))
            {
                pngEncoder.Save(fs);
            }

            pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(detectCrop));
            using (var fs = System.IO.File.OpenWrite(string.Format("{0}{1}-detect-{2}-{3}-{4}.png", outputfolder, filename, epsilon, reusedEdges, reusedVertices)))
            {
                pngEncoder.Save(fs);
            }
            SaveOutput(string.Format("{0}{1}-output-{2}.txt", outputfolder, filename, epsilon));

            pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(combinedImg));
            using (var fs = System.IO.File.OpenWrite(string.Format("{0}{1}-full-{2}-{3}.png", outputfolder, filename, epsilon, pg.GetArea())))
            {
                pngEncoder.Save(fs);
            }

            dg = new DrawingGroup();
            dg.Children.Add(id1);
            dg.Children.Add(id2);
            dg.Children.Add(id3);

            combinedImg = new RenderTargetBitmap((int)gridCrop.Width, (int)gridCrop.Height, 96, 96, PixelFormats.Pbgra32);
            dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawDrawing(dg);
            }
            combinedImg.Render(dv);

            pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(combinedImg));
            using (var fs = System.IO.File.OpenWrite(string.Format("{0}{1}-result-{2}.png", outputfolder, filename, epsilon)))
            {
                pngEncoder.Save(fs);
            }

            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            init();

            Draw(GridCanvas, _graph, Brushes.LightGray);
            Draw(InputCanvas, _path, Brushes.Black);
        }

        private void MenuItemClear_Click(object sender, RoutedEventArgs e)
        {
            _path = new Graph(0);

            _result = null;

            lblEpsilon.Content = '-';

            InputCanvas.Children.Clear();
            OutputCanvas.Children.Clear();
            DetectCanvas.Children.Clear();
        }

        private void DirectSameEdge_Unchecked(object sender, RoutedEventArgs e)
        {
            _allowSameEdge = false;

            OutputPolygonCanvas.Children.Clear();
            DetectCanvas.Children.Clear();
            OutputCanvas.Children.Clear();
            RenderCanvas.Children.Clear();
        }

        private void DirectSameEdge_Checked(object sender, RoutedEventArgs e)
        {
            _allowSameEdge = true;

            //ReDraw();
            OutputCanvas.Children.Clear();
            OutputPolygonCanvas.Children.Clear();
            DetectCanvas.Children.Clear();
            RenderCanvas.Children.Clear();
        }

        private void MenuItemSave_Click(object sender, RoutedEventArgs e)
        {      
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < _path.V.Length; i++)
            {
                stringBuilder.AppendLine(String.Format("{0} {1}",
                    _path.V[i].X.ToString(CultureInfo.CreateSpecificCulture("en-US")),
                    _path.V[i].Y.ToString(CultureInfo.CreateSpecificCulture("en-US"))));
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt";
            if (saveFileDialog.ShowDialog() == true)
                File.WriteAllText(saveFileDialog.FileName, stringBuilder.ToString());
        }

        private void SaveOutput(String filename)
        {
            // Convert path to string
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < _path.V.Length; i++)
            {
                stringBuilder.AppendLine(String.Format("{0} {1}",
                    _path.V[i].X.ToString(CultureInfo.CreateSpecificCulture("en-US")),
                    _path.V[i].Y.ToString(CultureInfo.CreateSpecificCulture("en-US"))));
            }

            File.WriteAllText(filename, stringBuilder.ToString());
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text file (*.txt)|*.txt|Ipe file (*.ipe)|*.ipe";
            if (openFileDialog.ShowDialog() == true)
            {
                string extension = System.IO.Path.GetExtension(openFileDialog.FileName);

                IFileReader fileReader = null;
                if (extension == ".ipe")
                {
                    fileReader = new IPEReader();
                }
                else
                {
                    fileReader = new TXTReader();
                }

                _path = fileReader.ReadFile(openFileDialog.FileName);

               
                currentFile = openFileDialog.SafeFileName;

                InputCanvas.Children.Clear();
                OutputCanvas.Children.Clear();
                DetectCanvas.Children.Clear();
                OutputPolygonCanvas.Children.Clear();
                RenderCanvas.Children.Clear();

                Normalize();
                Center();

                // Generate input polygon
                inputPolygon = new Polygon();
                foreach (Vertex v in _path.V)
                {
                    inputPolygon.Points.Add(new Point(v.X, v.Y));
                }

                Draw(InputCanvas, _path, Brushes.Black);

               
                //   Calculation();

                // ReDraw();
            }
        }

        private void Normalize()
        {
            if (_path == null)
                return;

            // Scale
            float xMin, xMax, yMin, yMax;
            xMin = xMax = _path.V.First().X;
            yMin = yMax = _path.V.First().Y;
            foreach (Vertex v in _path.V)
            {
                xMin = xMin < v.X ? xMin : v.X;
                xMax = xMax > v.X ? xMax : v.X;
                yMin = yMin < v.Y ? yMin : v.Y;
                yMax = yMax > v.Y ? yMax : v.Y;
            }

            // Scale factor
            float hScale = 44 / (xMax - xMin);
            float vScale = 44 /(yMax - yMin);

            float scale = hScale < vScale ? hScale : vScale;

            Console.WriteLine("scale: {0}", scale);

            // Scale
            foreach (Vertex v in _path.V)
            {
                v.X *= scale;
                v.Y *= scale;
            }

            Center();


        }

        private void Center()
        {
            if (_path == null)
                return;

            float xMin, xMax, yMin, yMax;
            xMin = xMax = _path.V.First().X;
            yMin = yMax = _path.V.First().Y;
            foreach (Vertex v in _path.V)
            {
                xMin = xMin < v.X ? xMin : v.X;
                xMax = xMax > v.X ? xMax : v.X;
                yMin = yMin < v.Y ? yMin : v.Y;
                yMax = yMax > v.Y ? yMax : v.Y;
            }

            // Center
            Vertex center = new Vertex((xMin + xMax) / 2, (yMin + yMax) / 2);
            foreach (Vertex v in _path.V)
            {
                v.X -= center.X - (44.0f / 2) - 3.0f;
                v.Y -= center.Y - (44.0f / 2) - 3.0f;
            }
        }

        private void MenuItemGrid_Click(object sender, RoutedEventArgs e)
        {
            GridBuilderWindow gridBuilderWindow = new GridBuilderWindow();
            if (gridBuilderWindow.ShowDialog() == true)
            {
                _graph = gridBuilderWindow.Grid;

                GridCanvas.Children.Clear();
                Draw(GridCanvas, _graph, Brushes.LightGray);

                Calculation();

                ReDraw();
            }
        }

        private CombinedGeometry cg;
        private PathGeometry pg;
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            cg = new CombinedGeometry(inputPolygon.RenderedGeometry, outputPolygon.RenderedGeometry);
            cg.GeometryCombineMode = GeometryCombineMode.Xor;
             pg = cg.GetFlattenedPathGeometry();
            System.Windows.Shapes.Path combinedPath = new System.Windows.Shapes.Path();
            combinedPath.Data = cg;
            combinedPath.Fill = Brushes.Red;
            Console.Out.WriteLine("Output area: {0}", inputPolygon.RenderedGeometry.GetArea());
            Console.Out.WriteLine("Symmetric difference: {0}", cg.GetArea());
            Console.Out.WriteLine("Symmetric difference: {0}", pg.GetArea());
            combinedPath.LayoutTransform = new ScaleTransform(10, 10, 0, 0);
            RenderCanvas.Children.Add(combinedPath);
        }
    }
}


