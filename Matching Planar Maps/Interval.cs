using System.Collections.Generic;

namespace Matching_Planar_Maps
{
    public class Interval : Range
    {
        public Interval()
        {
            Start = float.NaN;
            End = float.NaN;
        }

        public Interval(float x, float y)
        {
            Start = x;
            End = y;
        }

        public List<float> LeftPointers { get; set; } = new List<float>();

        public List<float> RightPointers { get; set; } = new List<float>();

        public Interval PathPointer { get; set; }

    }
}
