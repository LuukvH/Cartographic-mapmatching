using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matching_Planar_Maps
{
    class Test
    {
        Graph graph = new Graph();
        Graph path = new Graph();

        public Test()
        {
            // Instantiate a graph
            Vertex v1 = new Vertex(1, 1);
            Vertex v2 = new Vertex(4, 4);
            graph.V.Add(v1);
            graph.V.Add(v2);
            graph.E.Add(v1, v2);

            // Instantiate a graph
            Vertex p1 = new Vertex(1, 2);
            Vertex p2 = new Vertex(4, 2);
            path.V.Add(p1);
            path.V.Add(p2);
            path.E.Add(p1, p2);

        }
    }
}
