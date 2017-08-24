using System;

namespace Matching_Planar_Maps
{
    public class Edge
    {
        public Vertex V1, V2;

        public Edge(Vertex v1, Vertex v2)
        {
            this.V1 = v1;
            this.V2 = v2;
        }

        public Vertex Location(float param)
        {
            return V1 + (V2 - V1)*param;
        }

        public float LengthSquared => GraphFunctions.DistanceSquared(V1, V2);

        public float Location(Vertex p)
        {
            return (float)(Math.Sqrt(GraphFunctions.DistanceSquared(V1, p))/Math.Sqrt(LengthSquared));
        }
        

    }
}
