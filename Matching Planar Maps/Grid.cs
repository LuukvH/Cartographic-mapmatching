using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matching_Planar_Maps
{
    public class GridGraph : Graph
    {
        private int _xSize;
        private int _ySize;
        private float _spacing;

        public GridGraph(int xSize, int ySize, float spacing) : base(xSize*ySize)
        {
            this._xSize = xSize;
            this._ySize = ySize;
            this._spacing = spacing;
            GenerateGrid();
        }

        private int GridIndex(int xSize, int x, int y)
        {
            return xSize * y + x;
        }

        private void GenerateGrid()
        {
            // Create vertices
            for (int y = 0; y < _ySize; y++)
            {
                for (int x = 0; x < _xSize; x++)
                {
                    int index = GridIndex(_xSize, x, y);
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
                        E[index].Add(GridIndex(_xSize, x, y - 1));
                        E[GridIndex(_xSize, x, y - 1)].Add(index);
                    }
                }
            }

        }
    }
}
