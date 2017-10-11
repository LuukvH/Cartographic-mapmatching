using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Matching_Planar_Maps
{
    public class MapMatching
    {
        private const float TOLERANCE = 0.0000001f;
        public static bool _debug = true;

        // Preprocessing
        private List<Interval>[] FD;
        private List<Interval>[] B;
        private List<Interval>[,] L;

        public Interval Calculate(Graph graph, Path path, float epsilon, bool allowSameEdge)
        {
            GC.Collect();

            if (_debug)
                Console.WriteLine("Epsilon: {0}", epsilon);

            Preprocessing(graph, path, epsilon);

            Interval result = dynamicProgrammingStage(graph, path, allowSameEdge);

            if (_debug)
                Console.WriteLine(result != null ? "Feasable Path exists" : "No feasable Path exists");

            return result;
        }

        public void Preprocessing(Graph graph, Path path, float epsilon)
        {
            if (path == null || path.Size <= 1)
                return;

            var watch = new System.Diagnostics.Stopwatch();

            FD = new List<Interval>[graph.Size];
            B = new List<Interval>[graph.Size];
            if (_debug)
                Console.Write("Calculating B");
            watch.Reset();
            watch.Start();
            for (int i = 0; i < graph.Size; i++)
            {
                FD[i] = new List<Interval>();
                B[i] = new List<Interval>();
                for (int n = 0; n < path.E.Length; n++)
                {
                    // Continue if no outgoing edges
                    if (path.E[n].Count <= 0)
                        continue;

                    Interval interval = GraphFunctions.CalculateInterval(path.V[n], path.V[path.E[n][0]],
                            graph.V[i],
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
            watch.Stop();

            if (_debug)
                Console.WriteLine(" ({0} ms)", watch.ElapsedMilliseconds);

            // Calculate L
            if (_debug)
                Console.Write("Calculating L");

            watch.Reset();
            watch.Start();

            L = new List<Interval>[graph.Size, path.Size];
            for (int i = 0; i < graph.Size; i++)
            {
                for (int p = 0; p < path.Size; p++)
                {
                    L[i, p] = new List<Interval>(4);

                    for (int j = 0; j < graph.E[i].Count; j++)
                    {
                        // Validate if cell is reachable otherwise set interval empty
                        Interval interval = new Interval(1, 0);
                        if (p <= 0 || !L[i, p - 1][j].Empty() || !B[i][p - 1].Empty())
                        {
                            interval = GraphFunctions.CalculateInterval(graph.V[i], graph.V[graph.E[i][j]],
                                path.V[p], epsilon);
                        }

                        L[i, p].Add(interval);
                    }
                }
            }
            watch.Stop();

            if (_debug)
                Console.WriteLine(" ({0} ms)", watch.ElapsedMilliseconds);

            if (_debug)
                Console.Write("Calculating LeftPointers");

            watch.Reset();
            watch.Start();
            LeftPointers(graph, path);
            watch.Stop();

            if (_debug)
                Console.WriteLine(" ({0} ms)", watch.ElapsedMilliseconds);

            if (_debug)
                Console.Write("Calculating RightPointers");

            watch.Reset();
            watch.Start();
            RightPointers(graph, path);
            watch.Stop();

            if (_debug)
                Console.WriteLine(" ({0} ms)", watch.ElapsedMilliseconds);
        }

        public void LeftPointers(Graph graph, Path path)
        {
            for (int i = 0; i < graph.Size; i++)
            {
                for (int j = 0; j < graph.E[i].Count; j++)
                {
                    // For each white intervall in FDi calculate left pointers
                    foreach (Interval interval in FD[i])
                    {

                        float leftpointer = float.NaN;
                        int k = interval.PathIndex;
                        float max_ai = 0; //L[i, k][j].Start;

                        if (B[i][k].Start <= B[graph.E[i][j]][k].End && !B[graph.E[i][j]][k].Empty())
                        {
                            leftpointer = Math.Max(B[i][k].Start, B[graph.E[i][j]][k].Start);
                        }
                        else
                        {
                            for (int n = k + 1; n < path.Size - 1; n++)
                            {
                                // Can move to this cell in a monetone indexPath
                                if (!L[i, n][j].Empty() && L[i, n][j].End >= max_ai)
                                {
                                    if (B[graph.E[i][j]][n].Start > B[graph.E[i][j]][n].End)
                                    {
                                        max_ai = Math.Max(L[i, n][j].Start, max_ai);
                                    }
                                    else
                                    {
                                        leftpointer = B[graph.E[i][j]][n].Start;
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

        public void RightPointers(Graph graph, Path path)
        {
            for (int i = 0; i < graph.Size; i++)
            {
                if (!FD[i].Any())
                    continue;

                for (int j = 0; j < graph.E[i].Count; j++)
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
                    for (int k = 0; k < path.Size - 1; k++)
                    {
                        kp = k;

                        while (kp < path.Size - 1)
                        {
                            if (ai1 > L[i, kp + 1][j].End || kp + 1 == path.Size - 1 || L[i, kp + 1][j].Empty())
                            {
                                // Maximal kp that fulfills (1)
                                int w = kp;

                                // Search white point to the left of kp + 1;
                                while (w > 0)
                                {
                                    if (!B[graph.E[i][j]][w].Empty())
                                    {
                                        break;
                                    }
                                    w--;
                                }

                                if (k < w)
                                {
                                    B[i][k].RightPointers.Add(B[graph.E[i][j]][w].End);
                                }
                                else if (k > w)
                                {
                                    B[i][k].RightPointers.Add(float.NaN);
                                }
                                else
                                {
                                    if (!B[graph.E[i][j]][k].Empty() && B[i][k].Start <= B[graph.E[i][j]][k].End)
                                    {
                                        B[i][k].RightPointers.Add(B[graph.E[i][j]][k].End);
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

        public Interval dynamicProgrammingStage(Graph graph, Path path, bool allowSameEdge)
        {

            //////////////////////////
            // Initialization phase //
            //////////////////////////
            List<Interval> Q = new List<Interval>();
            Range[] C = new Range[graph.Size];
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
            for (int i = 0; i < graph.Size; i++)
            {
                if (FD[i].Count > 0 && !FD[i][0].Empty() && FD[i][0].Start < TOLERANCE)
                {
                    Q.Add(FD[i][0]);
                    C[i].Start = FD[i][0].Start;
                    C[i].End = FD[i][0].End;
                }
            }

            // If q is not empty continue otherwise no indexPath exists
            while (Q.Any() && result == null)
            {
                // Sort q to priority
                Q = Q.OrderBy(q => q.Start).ToList();

                // Step 1 extract leftmost interval
                Interval I = Q.First(); // Get first interval
                Q.Remove(Q.First()); // Remove fist interval
                x = I.Start; // Advance x to l(I)

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
                for (int j = 0; j < graph.E[I.GraphIndex].Count; j++)
                {

                    Range cj = C[graph.E[I.GraphIndex][j]];
                    float lfttend = cj.Start;

                    // Disallow going back on same edge
                    if (I.PathPointer != null && graph.E[I.GraphIndex][j] == I.PathPointer.GraphIndex && !allowSameEdge)
                        continue;

                    if (float.IsNaN(I.LeftPointers[j]))
                        continue;

                    if (I.LeftPointers[j] > cj.End || float.IsNaN(cj.End))
                    {
                        case1++;

                        C[graph.E[I.GraphIndex][j]] = new Range(I.LeftPointers[j], I.RightPointers[j]);
                        C[graph.E[I.GraphIndex][j]].GraphIndex = cj.GraphIndex;
                        cj = C[graph.E[I.GraphIndex][j]];

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
                        for (int i = 0; i < FD[graph.E[I.GraphIndex][j]].Count; i++)
                        {

                            if (FD[graph.E[I.GraphIndex][j]][i].Start >= cj.Start && FD[graph.E[I.GraphIndex][j]][i].Start <= cj.End)
                            {
                                if (first)
                                {
                                    Q.Add(FD[graph.E[I.GraphIndex][j]][i]);
                                    first = false;
                                }

                                if (Math.Abs(FD[graph.E[I.GraphIndex][j]][i].Start) > TOLERANCE)
                                {
                                    FD[graph.E[I.GraphIndex][j]][i].PathPointer = FD[graph.E[I.GraphIndex][j]][i].PathPointer ?? I;
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
                        for (int i = 0; i < FD[graph.E[I.GraphIndex][j]].Count; i++)
                        {

                            if (FD[graph.E[I.GraphIndex][j]][i].Start >= cj.Start && FD[graph.E[I.GraphIndex][j]][i].Start <= cj.End)
                            {
                                if (first)
                                {
                                    Q.Add(FD[graph.E[I.GraphIndex][j]][i]);
                                    first = false;
                                }

                                if (Math.Abs(FD[graph.E[I.GraphIndex][j]][i].Start) > TOLERANCE)
                                {
                                    FD[graph.E[I.GraphIndex][j]][i].PathPointer = FD[graph.E[I.GraphIndex][j]][i].PathPointer ?? I;
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
                        for (int i = 0; i < FD[graph.E[I.GraphIndex][j]].Count; i++)
                        {

                            if (FD[graph.E[I.GraphIndex][j]][i].Start >= cj.Start && FD[graph.E[I.GraphIndex][j]][i].Start <= cj.End)
                            {
                                if (first)
                                {
                                    Q.Add(FD[graph.E[I.GraphIndex][j]][i]);
                                    first = false;
                                }

                                if (Math.Abs(FD[graph.E[I.GraphIndex][j]][i].Start) > TOLERANCE)
                                {
                                    FD[graph.E[I.GraphIndex][j]][i].PathPointer = FD[graph.E[I.GraphIndex][j]][i].PathPointer ?? I;
                                }
                            }
                        }
                    }

                    // Set result if endpoint reached
                    // Console.WriteLine("Rightpointer values: " + rightPointers[cj.PathIndex, cj.GraphIndex]);
                    if (cj.End >= path.Size - 1 - TOLERANCE)
                    {
                        //result = I;
                        try
                        {
                            result = Q.First(i => i.GraphIndex == cj.GraphIndex);
                        }
                        catch
                        {
                        }
                    }

                }
            }

            //Console.WriteLine("Case1: " + case1);
            //Console.WriteLine("Case2: " + case2);
            //Console.WriteLine("Case3: " + case3);

            return result;
        }

        public List<FreeSpaceStrip>[] GenerateFreeSpaceStrips(Graph graph, Path path, int size, float epsilon)
        {
            // Generate free space strips
            List<FreeSpaceStrip>[] _freeSpaceStrips = new List<FreeSpaceStrip>[graph.Size];
            //freeSpaceStack.Children.Clear();

            for (int i = 0; i < graph.Size; i++)
            {
                _freeSpaceStrips[i] = new List<FreeSpaceStrip>(graph.Size * 4);

                // For every outgoing edge
                for (int j = 0; j < graph.E[i].Count; j++)
                {
                    FreeSpaceStrip freeSpaceStrip = new FreeSpaceStrip(i, graph.E[i][j], size);
                    //freeSpaceStrip.MouseEnter += freespacestrip_MouseEnter;
                    //freeSpaceStrip.MouseLeftButtonDown += freespacestrip_Click;

                    if (FD[i].Count > 0 || FD[graph.E[i][j]].Count > 0)
                    {
                        foreach (Interval interval in FD[i])
                        {
                            if (!float.IsNaN(interval.LeftPointers[j]) ||
                                !float.IsNaN(interval.RightPointers[j]))
                            {
                                freeSpaceStrip.wbmp = BitmapFactory.New(size * (path.Size - 1), size);
                                freeSpaceStrip.wbmp.Clear(Colors.Gray);
                                freeSpaceStrip.imgControl.Source = freeSpaceStrip.wbmp;

                                DrawFreeSpaceDiagram(freeSpaceStrip, graph, path, size, epsilon);
                                DrawIntervals(freeSpaceStrip, graph, path, size);
                                DrawLeftPointers(freeSpaceStrip, graph, path, size);
                                DrawRightPointers(freeSpaceStrip, graph, path, size);


                                //freeSpaceStack.Children.Insert(0, freeSpaceStrip);
                                freeSpaceStrip.active = true;
                                break;
                            }
                        }
                    }

                    _freeSpaceStrips[i].Add(freeSpaceStrip);
                }
            }
            return _freeSpaceStrips;
        }

        public static void DrawFreeSpaceDiagram(FreeSpaceStrip freeSpaceStrip, Graph graph, Path path, int size, float epsilon)
        {
            int i = freeSpaceStrip.I;
            int j = graph.E[i].IndexOf(freeSpaceStrip.J);

            freeSpaceStrip.Canvas.Width = size * path.Size;
            // For every edge in indexPath
            for (int n = 0; n < path.Size - 1; n++)
            {
                if (path.E[n].Count <= 0)
                    continue;

                int steps = size;
                for (int s = 0; s < steps; s++)
                {
                    float loc = ((1f / steps) * s);
                    Vertex c = path.V[n] + (path.V[path.E[n][0]] - path.V[n]) * loc;

                    Interval interval = GraphFunctions.CalculateInterval(graph.V[i], graph.V[graph.E[i][j]], c, epsilon);
                    if (!interval.Empty())
                    {
                        int X1 = Convert.ToInt32((loc + n) * size);
                        int Y1 = Convert.ToInt32(Inv(interval.Start * size, size));
                        int X2 = Convert.ToInt32((loc + n) * size);
                        int Y2 = Convert.ToInt32(Inv(interval.End * size, size));

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

        public static float Inv(float val, int size)
        {
            return (val - size) * -1;
        }

        public void DrawIntervals(FreeSpaceStrip freeSpaceStrip, Graph graph, Path path, int size)
        {
            int i = freeSpaceStrip.I;
            int j = graph.E[i].IndexOf(freeSpaceStrip.J);

            // FD intervals
            foreach (Interval interval in FD[i])
            {
                int X1 = Convert.ToInt32(interval.Start * size);
                int Y1 = Convert.ToInt32(size);
                int X2 = Convert.ToInt32(interval.End * size);
                int Y2 = Convert.ToInt32(size);
                freeSpaceStrip.wbmp.DrawLine(X1, Y1, X2, Y2, Colors.Blue);
            }

            foreach (Interval interval in FD[graph.E[i][j]])
            {
                if (!interval.Empty())
                {
                    int X1 = Convert.ToInt32(interval.Start * size);
                    int Y1 = Convert.ToInt32(0);
                    int X2 = Convert.ToInt32(interval.End * size);
                    int Y2 = Convert.ToInt32(0);
                    freeSpaceStrip.wbmp.DrawLine(X1, Y1, X2, Y2, Colors.Blue);
                }
            }

            // L intervals
            for (int p = 0; p < path.Size; p++)
            {
                Interval interval = L[i, p][j];
                if (!interval.Empty())
                {
                    int X1 = Convert.ToInt32(size * p);
                    int Y1 = Convert.ToInt32(Inv(interval.Start * size, size));
                    int X2 = Convert.ToInt32(size * p);
                    int Y2 = Convert.ToInt32(Inv(interval.End * size, size));
                    freeSpaceStrip.wbmp.DrawLine(X1, Y1, X2, Y2, Colors.Blue);
                }
            }
        }

        public void DrawLeftPointers(FreeSpaceStrip freeSpaceStrip, Graph graph, Path path, int size)
        {
            int i = freeSpaceStrip.I;
            int j = graph.E[i].IndexOf(freeSpaceStrip.J);

            // FD intervals
            foreach (Interval interval in FD[i])
            {
                if (float.IsNaN(interval.LeftPointers[j]))
                    continue;

                int X1 = Convert.ToInt32(interval.Start * size);
                int Y1 = Convert.ToInt32(size);
                int X2 = Convert.ToInt32(interval.LeftPointers[j] * size);
                int Y2 = Convert.ToInt32(0);
                freeSpaceStrip.wbmp.DrawLine(X1, Y1, X2, Y2, Colors.Green);
            }
        }

        public void DrawRightPointers(FreeSpaceStrip freeSpaceStrip, Graph graph, Path path, int size)
        {
            int i = freeSpaceStrip.I;
            int j = graph.E[i].IndexOf(freeSpaceStrip.J);

            // FD intervals
            foreach (Interval interval in FD[i])
            {
                if (float.IsNaN(interval.RightPointers[j]))
                    continue;

                int X1 = Convert.ToInt32(interval.Start * size);
                int Y1 = Convert.ToInt32(size);
                int X2 = Convert.ToInt32(interval.RightPointers[j] * size);
                int Y2 = Convert.ToInt32(0);
                freeSpaceStrip.wbmp.DrawLine(X1, Y1, X2, Y2, Colors.Red);
            }
        }
    }
}
