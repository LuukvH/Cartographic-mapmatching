using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Matching_Planar_Maps
{
    public class IPEReader : IFileReader
    {

        public Path ReadFile(String file)
        {
            XDocument doc = XDocument.Load(file);

            var page = doc.Descendants("page").First();
            var paths = page.Descendants("path");

            var pathData = paths.First();

            List<Vertex> vertices = new List<Vertex>();
            using (StringReader reader = new StringReader(pathData.Value))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] coordinate = line.Split(' ');
                    if (coordinate.Length == 3)
                    {
                        vertices.Add(new Vertex(float.Parse(coordinate[0], CultureInfo.InvariantCulture.NumberFormat), float.Parse(coordinate[1], CultureInfo.InvariantCulture.NumberFormat) * -1));
                    } else if (coordinate.Length == 1)
                    {
                        if (coordinate[0].Trim() == "h")
                            vertices.Add(new Vertex(vertices[0].X, vertices[0].Y));
                    }
                }
            }

            // Add all vertices to graph
            Path path = new Path(vertices.Count);
            path.V = vertices.ToArray();

            return path;
        }
     }
}
