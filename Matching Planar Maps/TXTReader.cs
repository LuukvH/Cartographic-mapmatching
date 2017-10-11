using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Matching_Planar_Maps
{
    public class TXTReader : IFileReader
    {

        public Path ReadFile(String file)
        {
            String data = "";

            try
            {   // Open the text file using a stream reader.
                using (StreamReader sr = new StreamReader(file))
                {
                    // Read the stream to a string, and write the string to the console.
                    data = sr.ReadToEnd();
                 }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

            List<Vertex> vertices = new List<Vertex>();
            using (StringReader reader = new StringReader(data))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] coordinate = line.Split(' ');
                    if (coordinate.Length >= 2)
                    {
                       vertices.Add(new Vertex(float.Parse(coordinate[0], CultureInfo.InvariantCulture.NumberFormat), float.Parse(coordinate[1], CultureInfo.InvariantCulture.NumberFormat)));
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
