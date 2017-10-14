using System;
using System.Collections.Generic;
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
        private float spacing = 2.0f;
        private float rspacing = 2.0f;
        public List<int> vertices = new List<int>();
        public Graph PartialGraph = null;
        private Graph _graph;

        public PartialProblem(Graph graph)
        {
            _graph = graph;
        }

        public void addVertex(int index)
        {
            Vertex vertex = _graph.V[index];
            if (!vertices.Any())
            {
                lbox = vertex.X - spacing;
                rbox = vertex.X + rspacing;
                tbox = vertex.Y + spacing;
                bbox = vertex.Y - spacing;
            }
            else
            {
               lbox = vertex.X - spacing < lbox ? vertex.X - spacing : lbox;
               rbox = vertex.X + rspacing > rbox ? vertex.X + rspacing : rbox;
                tbox = vertex.Y + spacing > tbox ? vertex.Y + spacing: tbox;
                bbox = vertex.Y - spacing < bbox ? vertex.Y - spacing : bbox;
            }

            vertices.Add(index);
        }

        public Path getPath(Path path) {

            List<Vertex> pv = new List<Vertex>();
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

            Vertex intersectionend = findBoxIntersection(path, end-1, end);
            if (intersectionend != null)
                pv.Add(intersectionend);

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


        public List<List<int>> getPossiblePaths(int start, int end)
        {
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

            // If is on edge than add the path
            if (index == end)
                newPaths.Add(path);

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
