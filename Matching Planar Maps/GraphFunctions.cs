using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matching_Planar_Maps
{
    public static class GraphFunctions
    {

        public static float DistanceSquared(Vertex v1, Vertex v2)
        {
            return (float)(Math.Pow(v2.X - v1.X, 2) + Math.Pow(v2.Y - v1.Y, 2));
        }
    }
}
