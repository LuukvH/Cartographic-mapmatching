namespace Matching_Planar_Maps
{
    public class Path : Graph
    {
        public Path(int size) : base(size)
        {
            // Add all edges
            for (int i = 0; i < Size - 1; i++)
            {
                E[i].Add(i + 1);
            }
        }
   }
}
