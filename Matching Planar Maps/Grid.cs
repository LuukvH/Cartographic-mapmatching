namespace Matching_Planar_Maps
{
    public class GridGraph : Graph
    {
        private int _xSize;
        private int _ySize;
        public float _spacing;

        public GridGraph(int xSize, int ySize, float spacing) : base(xSize*ySize)
        {
            this._xSize = xSize;
            this._ySize = ySize;
            this._spacing = spacing;
            GenerateGrid();
        }

        public int GridIndex(int x, int y)
        {
            return _xSize * y + x;
        }

        public int GridX(int value)
        {
            return value - (value / _xSize) *_xSize;
        }

        public int GridY(int value)
        {
            return value/_xSize;
        }

        private void GenerateGrid()
        {
            // Create vertices
            for (int y = 0; y < _ySize; y++)
            {
                for (int x = 0; x < _xSize; x++)
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
