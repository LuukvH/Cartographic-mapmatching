using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
