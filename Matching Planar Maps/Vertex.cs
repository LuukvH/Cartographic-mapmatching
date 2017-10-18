using System;

namespace Matching_Planar_Maps
{
    public class Vertex
    {
        public float X, Y;
        private const float TOLERANCE = 0.0000001f;

        public Vertex()
        {
            X = float.NaN;
            Y = float.NaN;
        }

        public Vertex(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Vertex v = obj as Vertex;
            if ((System.Object)v == null)
            {
                return false;
            }

            // Return true if the fields match:
            if (Math.Abs(v.X - this.X) < TOLERANCE)
            {
                if (Math.Abs(v.Y - this.Y) < TOLERANCE)
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            return $"{this.X}, {this.Y}";
        }

        public static Vertex operator *(Vertex v1, float c)
        {
            return new Vertex(v1.X * c, v1.Y * c); 
        }

        public static Vertex operator +(Vertex v1, Vertex v2)
        {
            return new Vertex(v1.X + v2.X, v1.Y + v2.Y);
        }

        public static Vertex operator -(Vertex v1, Vertex v2)
        {
            return new Vertex(v1.X - v2.X, v1.Y - v2.Y);
        }
    }
}
