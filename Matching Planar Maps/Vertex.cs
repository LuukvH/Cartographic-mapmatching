namespace Matching_Planar_Maps
{
    public class Vertex
    {
        public float X, Y;

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
