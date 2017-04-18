
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matching_Planar_Maps
{
    public static class GraphFunctions
    {
        private const float TOLERANCE = 0.0000001f;

        public static float Distance(Vertex v1, Vertex v2)
        {
            return (float)Math.Sqrt(DistanceSquared(v1, v2));
        }

        public static float DistanceSquared(Vertex v1, Vertex v2)
        {
            return (float)(Math.Pow(v2.X - v1.X, 2) + Math.Pow(v2.Y - v1.Y, 2));
        }

        public static float Parameterization(Vertex v1, Vertex v2, Vertex p)
        {
            return (float)(Math.Sqrt(DistanceSquared(v1, p)) / Math.Sqrt(DistanceSquared(v1, v2)));
        }

        public static Interval CalculateInterval(Vertex v1, Vertex v2, Vertex c, float epsilon)
        {
            Vertex intersection1;
            Vertex intersection2;
            int nrOfIntersections = GraphFunctions.LineCircleIntersections(c.X, c.Y, epsilon, v1, v2, out intersection1,
                out intersection2);

            Interval interval = new Interval(1, 0);
            if (nrOfIntersections == 2)
            {
                // If both points are outside range return empty interval
                if (GraphFunctions.DistanceSquared(v1, c) < Math.Pow(epsilon, 2))
                {
                    intersection2.X = v1.X;
                    intersection2.Y = v1.Y;
                }
                else if (GraphFunctions.DistanceSquared(v2, c) < Math.Pow(epsilon, 2))
                {
                    intersection1.X = v2.X;
                    intersection1.Y = v2.Y;
                }

                interval.Start = GraphFunctions.Parameterization(v1, v2, intersection2);
                interval.End = GraphFunctions.Parameterization(v1, v2, intersection1);

                interval.Start = interval.Start > 1 ? 1 : interval.Start;
                interval.End = interval.End > 1 ? 1 : interval.End;
            }
            return interval;
        }

        public static int LineCircleIntersections(float cx, float cy, float radius,
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
                if (GraphFunctions.Distance(point1, intersection1) + GraphFunctions.Distance(point2, intersection1) > GraphFunctions.Distance(point1, point2))
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

                float test1 = GraphFunctions.Distance(point1, new Vertex(cx, cy));
                float test2 = GraphFunctions.Distance(point2, new Vertex(cx, cy));

                // Validate that this solution is on the line
                if (GraphFunctions.Distance(point1, new Vertex(cx, cy)) <= radius * (1 + TOLERANCE) && GraphFunctions.Distance(point2, new Vertex(cx, cy)) <= radius * (1 + TOLERANCE))
                    return 2;

                if (GraphFunctions.Distance(point1, intersection1) + GraphFunctions.Distance(point2, intersection1) > GraphFunctions.Distance(point1, point2) * (1 + TOLERANCE) &&
                    GraphFunctions.Distance(point1, intersection2) + GraphFunctions.Distance(point2, intersection2) > GraphFunctions.Distance(point1, point2) * (1 + TOLERANCE))
                {
                    intersection1 = new Vertex(float.NaN, float.NaN);
                    intersection2 = new Vertex(float.NaN, float.NaN);
                    return 0;
                }

                return 2;
            }
        }
    }
}
