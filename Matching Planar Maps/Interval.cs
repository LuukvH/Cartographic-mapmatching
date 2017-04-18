using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matching_Planar_Maps
{
    public class Interval
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

        public int PathIndex { get; set; }

        public int GraphIndex { get; set; }

        public Interval PathPointer { get; set; }

        public float Start { get; set; }

        public float End { get; set; }

        public bool Empty()
        {
            return Start >= End;
        }
    }
}
