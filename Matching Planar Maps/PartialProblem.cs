using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Matching_Planar_Maps
{
    public class PartialProblem
    {
        public float lbox, rbox, tbox, bbox = 0;
        private float rspacing = 1.0f;
        private float tspacing = 1.0f;
        private float bspacing = 2.0f;
        private float lspacing = 2.0f;
        public List<int> vertices = new List<int>();
        public Graph PartialGraph = null;
        private Graph _graph;

        public int Start;
        public int End;

        public static int count = 0;
        public static int pathlength = 0;


        public PartialProblem(Graph graph)
        {
            _graph = graph;
        }

        public void addVertex(int index)
        {
            Vertex vertex = _graph.V[index];
            if (!vertices.Any())
            {
                lbox = vertex.X - lspacing;
                rbox = vertex.X + rspacing;
                tbox = vertex.Y + bspacing;
                bbox = vertex.Y - tspacing;
            }
            else
            {
                lbox = vertex.X - lspacing < lbox ? vertex.X - lspacing : lbox;
                rbox = vertex.X + rspacing > rbox ? vertex.X + rspacing : rbox;
                tbox = vertex.Y + bspacing > tbox ? vertex.Y + bspacing : tbox;
                bbox = vertex.Y - tspacing < bbox ? vertex.Y - tspacing : bbox;
            }

           vertices.Add(index);
        }

        public Path getPath(Path path)
        {

            List<Vertex> pv = new List<Vertex>();

            try
            {
                int i = 0;
                int start = -1;
                int end = -1;
                while (!isInside(path.V[i]))
                {
                    i++;
                }
                start = i;
                while (isInside(path.V[i]))
                {
                    i++;
                }
                end = i;

                Vertex intersectionstart = findBoxIntersection(path, start - 1, start);
                if (intersectionstart != null)
                    pv.Add(intersectionstart);

                for (int n = start; n < end; n++)
                {
                    pv.Add(path.V[n]);
                }

                Vertex intersectionend = findBoxIntersection(path, end - 1, end);
                if (intersectionend != null)
                {
                    // Do not add vertex when on top of already last vertex
                    if (!intersectionend.Equals(pv.Last()))
                        pv.Add(intersectionend);
                }


                // Remove duplicate vertices
            }
            catch
            {
                // ignored
            }

            //Console.Out.WriteLine($"Not Inside {i} ({path.V[i].X},{path.V[i].Y})");
            Path partialPath = new Path(pv.Count);
            partialPath.V = pv.ToArray();



            return partialPath;
        }

        private Vertex findBoxIntersection(Path path, int start, int end)
        {
            bool line_intersect = false;
            bool segment_intersect = false;
            Vertex intersection = new Vertex();
            Vertex close_p1 = new Vertex();
            Vertex close_p2 = new Vertex();

            GraphFunctions.FindIntersection(path.V[start], path.V[end], new Vertex(lbox, tbox),
    new Vertex(rbox, tbox), out line_intersect, out segment_intersect, out intersection, out close_p1,
    out close_p2);

            if (!segment_intersect)
            {
                GraphFunctions.FindIntersection(path.V[start], path.V[end], new Vertex(lbox, bbox),
                    new Vertex(rbox, bbox), out line_intersect, out segment_intersect, out intersection, out close_p1,
                    out close_p2);
            }

            if (!segment_intersect)
            {
                GraphFunctions.FindIntersection(path.V[start], path.V[end], new Vertex(lbox, tbox), new Vertex(lbox, bbox), out line_intersect, out segment_intersect, out intersection, out close_p1,
                    out close_p2);
            }

            if (!segment_intersect)
            {
                GraphFunctions.FindIntersection(path.V[start], path.V[end], new Vertex(rbox, tbox), new Vertex(rbox, bbox), out line_intersect, out segment_intersect, out intersection, out close_p1,
                    out close_p2);
            }

            if (segment_intersect)
            {
                return intersection;
            }
            return null;
        }

        public bool isInside(Vertex v)
        {
            return v.X >= lbox && v.X <= rbox && v.Y <= tbox && v.Y >= bbox;
        }

        public Graph partialGraph()
        {
            Graph partialGraph = new Graph(_graph.Size);
            partialGraph.V = _graph.V;

            for (int i = 0; i < _graph.Size; i++)
            {
                if (isInside(_graph.V[i]))
                {
                    for (int n = 0; n < _graph.E[i].Count; n++)
                    {
                        if (isInside(_graph.V[_graph.E[i][n]]))
                        {
                            partialGraph.E[i].Add(_graph.E[i][n]);
                        }
                    }
                }
            }

            PartialGraph = partialGraph;

            return partialGraph;
        }

        public List<List<int>> getPossiblePaths()
        {
            return getPossiblePaths(Start, End);
        }


        public List<List<int>> getPossiblePaths(int start, int end)
        {
            count = 0;
            List<List<int>> possiblePaths = new List<List<int>>();


            List<int> path = new List<int>();
            path.Add(start);
            possiblePaths = allPaths(path, end);
            Console.Out.WriteLine($"Possible paths: {possiblePaths.Count}");


            return possiblePaths;
        }

        public List<List<int>> allPaths(List<int> path, int end)
        {
            int index = path.Last();

            List<List<int>> newPaths = new List<List<int>>();

            // Stop when to long
            if (path.Count >= 30)
                return newPaths;

            // If is on edge than add the path
            if (index == end)
            {
                count++;

                // Print path length
                if (path.Count > pathlength)
                {
                    pathlength = path.Count;
                    Console.Out.WriteLine($"Path length {path.Count}");
                }

                if (count % 1000 == 0)
                {
                    Console.Out.WriteLine($"Possible paths: {count}");
                }

                newPaths.Add(path);
                return newPaths;
            }

            for (int i = 0; i < PartialGraph.E[index].Count; i++)
            {
                if (!path.Contains(PartialGraph.E[index][i]))
                {
                    List<int> newpath = new List<int>();
                    newpath.AddRange(path);
                    newpath.Add(PartialGraph.E[index][i]);

                    newPaths.AddRange(allPaths(newpath, end));
                }
            }



            return newPaths;
        }

    }
}
