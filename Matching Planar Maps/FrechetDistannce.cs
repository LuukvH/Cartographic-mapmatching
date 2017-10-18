using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Matching_Planar_Maps
{

    class FrechetDistance
    {
        private const float TOLERANCE = 0.0000001f;
        public static bool _debug = false;

        // Preprocessing
        private List<Range>[] B;
        private List<Range>[,] L;

        private Range[,] br;
        private Range[,] lr;

        private Path _path;
        private Graph _graph;

        private float _epsilon = 0.0f;

        public FrechetDistance(Graph graph, Path path)
        {
            this._graph = graph;
            this._path = path;
        }

        public void Preprocessing(float epsilon)
        {
            _epsilon = epsilon;

            var watch = new System.Diagnostics.Stopwatch();

            B = new List<Range>[_graph.Size];
            if (_debug)
                Console.Write("Calculating B");
            watch.Reset();
            watch.Start();
            for (int i = 0; i < _graph.Size; i++)
            {
                B[i] = new List<Range>();
                for (int n = 0; n < _path.Size - 1; n++)
                {

                    Range interval = GraphFunctions.CalculateInterval(_path.V[n], _path.V[_path.E[n][0]],
                            _graph.V[i],
                            epsilon);

                    // Offset with index
                    interval.Start += n;
                    interval.End += n;
                    interval.PathIndex = n;
                    interval.GraphIndex = i;

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

            L = new List<Range>[_graph.Size, _path.Size];
            for (int i = 0; i < _graph.Size; i++)
            {
                for (int p = 0; p < _path.Size; p++)
                {
                    L[i, p] = new List<Range>(4);

                    for (int j = 0; j < _graph.E[i].Count; j++)
                    {
                        // Validate if cell is reachable otherwise set interval empty
                        Range interval = new Range(1, 0);
                        interval = GraphFunctions.CalculateInterval(_graph.V[i], _graph.V[_graph.E[i][j]],
                            _path.V[p], epsilon);

                        L[i, p].Add(interval);
                    }
                }
            }
            watch.Stop();

            if (_debug)
                Console.WriteLine(" ({0} ms)", watch.ElapsedMilliseconds);
        }

        public bool Calculate(List<int> indexPath, float epsilon)
        {
            Console.Out.WriteLine($"Calculating frechet: {epsilon}");
            Preprocessing(epsilon);
            return FeasiblePath(indexPath);
        }

        public bool FeasiblePath(List<int> indexPath)
        {
            br = new Range[_path.Size, indexPath.Count];
            lr = new Range[_path.Size, indexPath.Count];

            for (int i = 0; i < _path.Size; i++)
            {
                for (int j = 0; j < indexPath.Count; j++)
                {
                    br[i, j] = new Range(1, 0);
                    lr[i, j] = new Range(1, 0);
                }
            }

            // Init br
            for (int i = 0; i < _path.Size - 1; i++)
            {
                if (i == 0)
                {
                    br[i, 0].Start = B[indexPath[0]][i].Start;
                    br[i, 0].End = B[indexPath[0]][i].End;
                }
                else
                {
                    br[i, 0].Start = 1;
                    br[i, 0].End = 0;
                }
            }

            // Init lr
            for (int i = 0; i < indexPath.Count - 1; i++)
            {
                if (i == 0)
                {
                    int e = _graph.E[indexPath[i]].IndexOf(indexPath[i + 1]);
                    lr[0, i].Start = L[indexPath[i], 0][e].Start;
                    lr[0, i].End = L[indexPath[i], 0][e].End;
                }
                else
                {
                    lr[0, i].Start = 1;
                    lr[0, i].End = 0;
                }
            }

            for (int i = 0; i < _path.Size - 1; i++)
            {
                for (int j = 0; j < indexPath.Count - 1; j++)
                {

                    int e = _graph.E[indexPath[j]].IndexOf(indexPath[j + 1]);
                    if (!br[i, j].Empty())
                    {
                        lr[i + 1, j].Start = L[indexPath[j], i + 1][e].Start;
                        lr[i + 1, j].End = L[indexPath[j], i + 1][e].End;
                    }
                    else
                    {
                        if (!lr[i, j].Empty())
                        {
                            lr[i + 1, j].Start = Math.Max(lr[i, j].Start, L[indexPath[j], i + 1][e].Start);
                            lr[i + 1, j].End = L[indexPath[j], i + 1][e].End;
                        }
                    }


                    if (!lr[i, j].Empty())
                    {
                        br[i, j + 1].Start = B[indexPath[j + 1]][i].Start;
                        br[i, j + 1].End = B[indexPath[j + 1]][i].End;
                    }
                    else
                    {
                        if (!br[i, j].Empty())
                        {
                            br[i, j + 1].Start = Math.Max(br[i, j].Start, B[indexPath[j + 1]][i].Start);
                            br[i, j + 1].End = B[indexPath[j + 1]][i].End;
                        }

                    }
                }
            }

            if (br[_path.Size - 2, indexPath.Count - 1].End >= _path.Size - 1 && br[0, 0].Start <= 0)
            {
                return true;
            }
            return false;
        }

        public List<FreeSpaceStrip> GenerateFreeSpaceStrips(List<int> indexPath, int size)
        {
            FeasiblePath(indexPath);


            // Generate free space strips
            List<FreeSpaceStrip> freeSpaceStrips = new List<FreeSpaceStrip>();

            // For every outgoing edge
            for (int i = 0; i < indexPath.Count - 1; i++)
            {
                FreeSpaceStrip freeSpaceStrip = new FreeSpaceStrip(indexPath[i], indexPath[i + 1], size);
                //freeSpaceStrip.MouseEnter += freespacestrip_MouseEnter;
                //freeSpaceStrip.MouseLeftButtonDown += freespacestrip_Click;

                freeSpaceStrip.wbmp = BitmapFactory.New(size * (_path.Size - 1), size);
                freeSpaceStrip.wbmp.Clear(Colors.Gray);
                freeSpaceStrip.imgControl.Source = freeSpaceStrip.wbmp;

                DrawFreeSpaceDiagram(freeSpaceStrip, indexPath, size);
                DrawIntervals(freeSpaceStrip, i, size);
                //DrawIntervals(freeSpaceStrip, graph, path, size);

                //freeSpaceStack.Children.Insert(0, freeSpaceStrip);
                freeSpaceStrip.active = true;




                freeSpaceStrips.Add(freeSpaceStrip);

            }
            return freeSpaceStrips;
        }

        private void DrawFreeSpaceDiagram(FreeSpaceStrip freeSpaceStrip, List<int> indexPath, int size)
        {
            int i = freeSpaceStrip.I;
            int j = _graph.E[i].IndexOf(freeSpaceStrip.J);

            freeSpaceStrip.Canvas.Width = size * _path.Size;
            // For every edge in indexPath
            for (int n = 0; n < _path.Size - 1; n++)
            {
                int steps = size;
                for (int s = 0; s < steps; s++)
                {
                    float loc = ((1f / steps) * s);
                    Vertex c = _path.V[n] + (_path.V[_path.E[n][0]] - _path.V[n]) * loc;

                    Interval interval = GraphFunctions.CalculateInterval(_graph.V[i], _graph.V[_graph.E[i][j]], c,
                        _epsilon);
                    if (!interval.Empty())
                    {
                        int X1 = Convert.ToInt32((loc + n) * size);
                        int Y1 = Convert.ToInt32(Inv(interval.Start * size, size));
                        int X2 = Convert.ToInt32((loc + n) * size);
                        int Y2 = Convert.ToInt32(Inv(interval.End * size, size));

                        freeSpaceStrip.wbmp.DrawLine(X1, Y1, X2, Y2, Colors.White);
                    }
                }
            }
        }

        private void DrawIntervals(FreeSpaceStrip freeSpaceStrip, int j, int size)
        {

            // FD intervals
            for (int i = 0; i < _path.Size - 1; i++)
            {
                Range range = br[i, j];

                if (!range.Empty())
                {
                    int X1 = Convert.ToInt32(range.Start * size);
                    int Y1 = Convert.ToInt32(size - 2);
                    int X2 = Convert.ToInt32(range.End * size);
                    int Y2 = Convert.ToInt32(size - 2);
                    //Console.Out.WriteLine($"({X1},{Y1}) ({X2},{Y2})");
                    freeSpaceStrip.wbmp.DrawLine(X1, Y1, X2, Y2, Colors.Blue);
                }
            }

            // L intervals
            for (int p = 0; p < _path.Size - 1; p++)
            {
                Range range = lr[p, j];

                if (!range.Empty())
                {
                    int X1 = Convert.ToInt32(size * p);
                    int Y1 = Convert.ToInt32(Inv(range.Start * size, size));
                    int X2 = Convert.ToInt32(size * p);
                    int Y2 = Convert.ToInt32(Inv(range.End * size, size));
                    freeSpaceStrip.wbmp.DrawLine(X1, Y1, X2, Y2, Colors.Blue);
                }
            }
        }


        private float Inv(float val, int size)
        {
            return (val - size) * -1;
        }
    }
}
