namespace Matching_Planar_Maps
{
    public class Range
    {
        public Range()
        {
            Start = float.NaN;
            End = float.NaN;
        }

        public Range(float x, float y)
        {
            Start = x;
            End = y;
        }

        public int PathIndex { get; set; }

        public int GraphIndex { get; set; }

        public float Start { get; set; }

        public float End { get; set; }

        public bool Empty()
        {
            return Start >= End;
        }
    }
}
