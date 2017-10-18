using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Matching_Planar_Maps
{

    class FrechetDistanceSimple
    {
        private const float TOLERANCE = 0.000001f;
        public static bool _debug = true;

        // Preprocessing
        private Range[,] B;
        private Range[,] L;

        private Range[,] br;
        private Range[,] lr;

        private float _epsilon = 0.0f;

        public void Preprocessing(List<Vertex> p1, List<Vertex> p2, float epsilon)
        {
            _epsilon = epsilon;

            // Init values
            B = new Range[p1.Count, p2.Count];
            L = new Range[p1.Count, p2.Count];
            for (int i = 0; i < p1.Count; i++)
            {
                for (int j = 0; j < p2.Count; j++)
                {
                    B[i, j] = new Range(1, 0);
                    L[i, j] = new Range(1, 0);
                }
            }

            var watch = new System.Diagnostics.Stopwatch();

            if (_debug)
                Console.Write("Calculating B");
            watch.Reset();
            watch.Start();
            for (int i = 0; i < p1.Count; i++)
            {
                for (int n = 0; n < p2.Count - 1; n++)
                {

                    Range interval = GraphFunctions.CalculateInterval(p2[n], p2[n + 1],
                            p1[i],
                            epsilon);

                    // Offset with index
                    interval.Start += n;
                    interval.End += n;

                    B[i, n] = interval;
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


            for (int i = 0; i < p1.Count - 1; i++)
            {
                for (int p = 0; p < p2.Count; p++)
                {
                    Range interval = new Range(1, 0);
                    interval = GraphFunctions.CalculateInterval(p1[i], p1[i + 1], p2[p], epsilon);

                    L[i, p] = interval;
                }
            }
            watch.Stop();

            if (_debug)
                Console.WriteLine(" ({0} ms)", watch.ElapsedMilliseconds);
        }

        public bool Calculate(List<Vertex> p1, List<Vertex> p2, float epsilon)
        {
            if (_debug)
                Console.Out.WriteLine($"Calculating frechet: {epsilon}");

            Preprocessing(p1, p2, epsilon);
            bool result = FeasiblePath(p1, p2);

            if (_debug)
                Console.WriteLine(result ? "Feasable Path exists" : "No feasable Path exists");

            return result;
        }

        public bool FeasiblePath(List<Vertex> p1, List<Vertex> p2)
        {
            br = new Range[p1.Count, p2.Count];
            lr = new Range[p1.Count, p2.Count];

            for (int i = 0; i < p1.Count; i++)
            {
                for (int j = 0; j < p2.Count; j++)
                {
                    br[i, j] = new Range(1, 0);
                    lr[i, j] = new Range(1, 0);
                }
            }

            // Init br
            br[0, 0].Start = B[0, 0].Start;
            br[0, 0].End = B[0, 0].End;

            // Init lr
            lr[0, 0].Start = L[0, 0].Start;
            lr[0, 0].End = L[0, 0].End;

            for (int i = 0; i < p1.Count - 1; i++)
            {
                for (int j = 0; j < p2.Count - 1; j++)
                {
                    if (!br[i, j].Empty())
                    {
                        lr[i, j + 1].Start = L[i, j + 1].Start;
                        lr[i, j + 1].End = L[i, j + 1].End;
                    }
                    else
                    {
                        if (!lr[i, j].Empty())
                        {
                            lr[i, j + 1].Start = Math.Max(lr[i, j].Start, L[i, j + 1].Start);
                            lr[i, j + 1].End = L[i, j + 1].End;
                        }
                    }

                    if (!lr[i, j].Empty())
                    {
                        br[i + 1, j].Start = B[i + 1, j].Start;
                        br[i + 1, j].End = B[i + 1, j].End;
                    }
                    else
                    {
                        if (!br[i, j].Empty())
                        {
                            br[i + 1, j].Start = Math.Max(br[i, j].Start, B[i + 1, j].Start);
                            br[i + 1, j].End = B[i + 1, j].End;
                        }
                    }

                }
            }

            if (Math.Abs(br[p1.Count - 1, p2.Count - 2].End - (p2.Count - 1)) < TOLERANCE && br[0, 0].Start <= 0)
            {
                return true;
            }
            return false;
        }


        public Image GenerateFreeSpaceDiagram(List<Vertex> p1, List<Vertex> p2, float epsilon, int size)
        {
            // Generate free space strips
            WriteableBitmap freeSpaceDiagram = BitmapFactory.New(size * p2.Count, size * p1.Count);
            freeSpaceDiagram.Clear(Colors.Gray);

            Image image = new Image();
            image.Source = freeSpaceDiagram;

            for (int i = 0; i < p1.Count - 1; i++)
            {
                for (int n = 0; n < p2.Count - 1; n++)
                {
                    int steps = size;
                    for (int s = 0; s < steps; s++)
                    {
                        float loc = ((1f / steps) * s);
                        Vertex c = p2[n] + (p2[n + 1] - p2[n]) * loc;

                        Interval interval = GraphFunctions.CalculateInterval(p1[i], p1[i + 1], c, epsilon);
                        if (!interval.Empty())
                        {
                            int X1 = Convert.ToInt32((loc + n) * size);
                            int Y1 = Convert.ToInt32(Inv(interval.Start * size, size)) + (p1.Count - 1 - i) * size;
                            int X2 = Convert.ToInt32((loc + n) * size);
                            int Y2 = Convert.ToInt32(Inv(interval.End * size, size)) + (p1.Count - 1 - i) * size;

                            freeSpaceDiagram.DrawLine(X1, Y1, X2, Y2, Colors.White);
                        }
                    }
                }
            }

            Preprocessing(p1, p2, epsilon);
            // FD intervals
            for (int i = 0; i < p1.Count; i++)
            {
                for (int j = 0; j < p2.Count - 1; j++)
                {
                    Range range = B[i, j];

                    if (!range.Empty())
                    {
                        int X1 = Convert.ToInt32(range.Start * size);
                        int Y1 = Convert.ToInt32(size - 2) + (p1.Count - 1 - i) * size;
                        int X2 = Convert.ToInt32(range.End * size);
                        int Y2 = Convert.ToInt32(size - 2) + (p1.Count - 1 - i) * size;
                        //Console.Out.WriteLine($"({X1},{Y1}) ({X2},{Y2})");
                        freeSpaceDiagram.DrawLine(X1, Y1, X2, Y2, Colors.Blue);
                    }
                }
            }

            for (int i = 0; i < p1.Count - 1; i++)
            {
                for (int j = 0; j < p2.Count - 1; j++)
                {
                    Range range = L[i, j];

                    if (!range.Empty())
                    {
                        int X1 = Convert.ToInt32(size * j);
                        int Y1 = Convert.ToInt32(Inv(range.Start * size, size)) + (p1.Count - 1 - i) * size;
                        int X2 = Convert.ToInt32(size * j);
                        int Y2 = Convert.ToInt32(Inv(range.End * size, size)) + (p1.Count - 1 - i) * size;
                        freeSpaceDiagram.DrawLine(X1, Y1, X2, Y2, Colors.Blue);
                    }
                }
            }

            // DrawIntervals
            if (FeasiblePath(p1, p2))
            {
                Console.Out.WriteLine($"Simple frechet: Feasible path exists");
            }
            else
            {
                Console.Out.WriteLine($"Simple frechet: Feasible path does not exists");
            }

            for (int i = 0; i < p1.Count; i++)
            {
                for (int j = 0; j < p2.Count - 1; j++)
                {
                    Range range = br[i, j];

                    if (!range.Empty())
                    {
                        int X1 = Convert.ToInt32(range.Start * size);
                        int Y1 = Convert.ToInt32(size - 2) + (p1.Count - 1 - i) * size;
                        int X2 = Convert.ToInt32(range.End * size);
                        int Y2 = Convert.ToInt32(size - 2) + (p1.Count - 1 - i) * size;
                        //Console.Out.WriteLine($"({X1},{Y1}) ({X2},{Y2})");
                        freeSpaceDiagram.DrawLine(X1, Y1, X2, Y2, Colors.Red);
                    }
                }
            }

            for (int i = 0; i < p1.Count - 1; i++)
            {
                for (int j = 0; j < p2.Count - 1; j++)
                {
                    Range range = lr[i, j];

                    if (!range.Empty())
                    {
                        int X1 = Convert.ToInt32(size * j);
                        int Y1 = Convert.ToInt32(Inv(range.Start * size, size)) + (p1.Count - 1 - i) * size;
                        int X2 = Convert.ToInt32(size * j);
                        int Y2 = Convert.ToInt32(Inv(range.End * size, size)) + (p1.Count - 1 - i) * size;
                        freeSpaceDiagram.DrawLine(X1, Y1, X2, Y2, Colors.Red);
                    }
                }
            }

            Console.Out.WriteLine(p2.Count);
            Console.Out.WriteLine();


            return image;
        }

        private float Inv(float val, int size)
        {
            return (val - size) * -1;
        }
    }
}
