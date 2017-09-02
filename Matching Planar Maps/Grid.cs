namespace Matching_Planar_Maps
{
    public class GridGraph : Graph
    {
        private int _size;
        public float _spacing;

        public GridGraph(int size, float spacing) : base((size + 1) * (size + 1))
        {
            this._size = size;
            this._spacing = spacing;
            GenerateGrid();
        }

        public int GridSize()
        {
            return _size;
        }

        public int GridIndex(int x, int y)
        {
            return (_size + 1) * y + x;
        }

        public int GridX(int value)
        {
            return value - (value / (_size + 1)) * (_size + 1);
        }

        public int GridY(int value)
        {
            return value/ (_size + 1);
        }

        private void GenerateGrid()
        {
            // Create vertices
            for (int y = 0; y < _size + 1; y++)
            {
                for (int x = 0; x < _size + 1; x++)
                {
                    int index = GridIndex(x, y);
                    Vertex v = new Vertex(x * _spacing, y * _spacing);
                    V[index] = v;

                    // connect to left
                    if (x > 0)
                    {
                        E[index].Add(index - 1);
                        E[index - 1].Add(index);
                    }

                    // Connect to above
                    if (y > 0)
                    {
                        E[index].Add(GridIndex(x, y - 1));
                        E[GridIndex(x, y - 1)].Add(index);
                    }
                }
            }

        }
    }
}
