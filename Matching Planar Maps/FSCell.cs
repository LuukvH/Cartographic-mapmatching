using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Matching_Planar_Maps
{
    public class FsCell
    {
        public FsCell(KeyValuePair<Vertex, Vertex> e1, KeyValuePair<Vertex, Vertex> e2, float epsilon)
        {
            LK = CalculateInterval(e1.Key.X, e1.Key.Y, epsilon, e2.Key, e2.Value);
            BK = CalculateInterval(e2.Key.X, e2.Key.Y, epsilon, e1.Key, e1.Value);
            //LKa = CalculateInterval(e1.Key.X, e1.Key.Y, epsilon, e2.Key, e2.Value);
            //BKa = CalculateInterval(e2.Key.X, e2.Key.Y, epsilon, e1.Key, e1.Value);
        }

        public Interval CalculateInterval(float cx, float cy, float radius, Vertex lineStart, Vertex lineEnd)
        {
            Vertex intersection1;
            Vertex intersection2;
            int intersections = FindLineCircleIntersections(cx, cy, radius, lineStart, lineEnd, out intersection1,
                out intersection2);

            if (intersections == 1)
            {
                float dist1 = DistanceSquared(intersection1, lineStart);
                return new Interval(0, dist1/DistanceSquared(lineStart, lineEnd)); //one intersection

            } else if (intersections == 2)
            {
                float dist1 = DistanceSquared(intersection1, lineStart);
                float dist2 = DistanceSquared(intersection2, lineStart);

                //if (dist1 < dist2)
                    return new Interval(dist1 / DistanceSquared(lineStart, lineEnd), dist2 / DistanceSquared(lineStart, lineEnd));
                //else
                  //  return new Interval(dist1 / DistanceSquared(lineStart, lineEnd), dist2 / DistanceSquared(lineStart, lineEnd));
            }

            return new Interval(1, 0); //one intersection
        }

        private float DistanceSquared(Vertex v1, Vertex p2)
        {
            return (float)(Math.Pow(p2.X - v1.X, 2) + Math.Pow(p2.Y - v1.Y, 2));
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

        public Interval LK { get; private set; }

        public Interval BK { get; private set; }

    }
}
