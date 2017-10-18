using System.Collections.Generic;

namespace Matching_Planar_Maps
{
    public class Graph
    {
        private Vertex[] _vertices = null;
        private List<int>[] _edges = null;
        private int _size = 0;

        public Graph(int size)
        {
            _size = size;

            _vertices = new Vertex[size];

            _edges = new List<int>[size];
            for (int i = 0; i < size; i++)
            {
                _edges[i] = new List<int>();
            }
        }

        public int Size
        {
            get { return _size; }
        }

        public Vertex[] V { get { return _vertices; } set { _vertices = value; } }

        public List<int>[] E => _edges;

        public void RemoveEdges(int v)
        {
            E[v].Clear();
            for (int i = 0; i < _size; i++)
            {
                E[i].Remove(v);
            }
        }
    }
}
