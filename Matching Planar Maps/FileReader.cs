using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Matching_Planar_Maps
{
    public class FileReader
    {

        public Graph test(String file)
        {
            XDocument doc = XDocument.Load(file);

            var page = doc.Descendants("page").First();
            var paths = page.Descendants("path");

            var path = paths.First();

            List<Vertex> vertices = new List<Vertex>();
            using (StringReader reader = new StringReader(path.Value))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] coordinate = line.Split(' ');
                    if (coordinate.Length == 3)
                    {
                        vertices.Add(new Vertex(float.Parse(coordinate[0], CultureInfo.InvariantCulture.NumberFormat), float.Parse(coordinate[1], CultureInfo.InvariantCulture.NumberFormat)));

                    }
                }
            }

            // Find x0 and y0
            float xmin = vertices.First().X;
            float ymax = vertices.First().X;
            foreach (Vertex t in vertices)
            {
                if (xmin > t.X)
                    xmin = t.X;

                if (ymax < t.X)
                    ymax = t.Y;
            }

            // Subtract offset
            for (int n = 0; n < vertices.Count; n++)
            {
                vertices[n].X = ((vertices[n].X - xmin) * 0.5f) + 30;
                vertices[n].Y = (((vertices[n].Y - ymax)*-1)*0.5f) + 30;
            }

            // Add all vertices to graph
            Graph graph = new Graph(vertices.Count);
            graph.V = vertices.ToArray();

            // Add all edges
            for (int i = 0; i < graph.Size - 1; i++)
            {
                graph.E[i].Add(i + 1);
            }

            return graph;
        }
     }
}
