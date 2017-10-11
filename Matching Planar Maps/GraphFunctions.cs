
using System;

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
            int nrOfIntersections = GraphFunctions.LineCircleIntersections(c, epsilon, v1, v2, out intersection1,
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

        public static void FindIntersection(
    Vertex p1, Vertex p2, Vertex p3, Vertex p4,
    out bool lines_intersect, out bool segments_intersect,
    out Vertex intersection,
    out Vertex close_p1, out Vertex close_p2)
        {
            // Get the segments' parameters.
            float dx12 = p2.X - p1.X;
            float dy12 = p2.Y - p1.Y;
            float dx34 = p4.X - p3.X;
            float dy34 = p4.Y - p3.Y;

            // Solve for t1 and t2
            float denominator = (dy12 * dx34 - dx12 * dy34);

            float t1 =
                ((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34)
                    / denominator;
            if (float.IsInfinity(t1))
            {
                // The lines are parallel (or close enough to it).
                lines_intersect = false;
                segments_intersect = false;
                intersection = new Vertex(float.NaN, float.NaN);
                close_p1 = new Vertex(float.NaN, float.NaN);
                close_p2 = new Vertex(float.NaN, float.NaN);
                return;
            }
            lines_intersect = true;

            float t2 =
                ((p3.X - p1.X) * dy12 + (p1.Y - p3.Y) * dx12)
                    / -denominator;

            // Find the point of intersection.
            intersection = new Vertex(p1.X + dx12 * t1, p1.Y + dy12 * t1);

            // The segments intersect if t1 and t2 are between 0 and 1.
            segments_intersect =
                ((t1 >= 0) && (t1 <= 1) &&
                 (t2 >= 0) && (t2 <= 1));

            // Find the closest points on the segments.
            if (t1 < 0)
            {
                t1 = 0;
            }
            else if (t1 > 1)
            {
                t1 = 1;
            }

            if (t2 < 0)
            {
                t2 = 0;
            }
            else if (t2 > 1)
            {
                t2 = 1;
            }

            close_p1 = new Vertex(p1.X + dx12 * t1, p1.Y + dy12 * t1);
            close_p2 = new Vertex(p3.X + dx34 * t2, p3.Y + dy34 * t2);
        }

        public static int LineCircleIntersections(Vertex c, float radius,
            Vertex point1, Vertex point2, out Vertex intersection1, out Vertex intersection2)
        {
            float dx, dy, A, B, C, det, t;

            dx = point2.X - point1.X;
            dy = point2.Y - point1.Y;

            A = dx * dx + dy * dy;
            B = 2 * (dx * (point1.X - c.X) + dy * (point1.Y - c.Y));
            C = (point1.X - c.X) * (point1.X - c.X) + (point1.Y - c.Y) * (point1.Y - c.Y) - radius * radius;

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

                float test1 = GraphFunctions.Distance(point1, c);
                float test2 = GraphFunctions.Distance(point2, c);

                // Validate that this solution is on the line
                if (GraphFunctions.Distance(point1, c) <= radius * (1 + TOLERANCE) && GraphFunctions.Distance(point2, c) <= radius * (1 + TOLERANCE))
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
