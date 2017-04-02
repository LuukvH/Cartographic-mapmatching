using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matching_Planar_Maps
{
    public class Interval
    {
        private float start, end;

        public Interval()
        {
            start = float.NaN;
            end = float.NaN;
        }

        public Interval(float x, float y)
        {
            Start = x;
            End = y > 1 ? 1 : y;
        }

        public float Start
        {
            get {  return start; }
            set { start = value > 1 ? 1 : value; }
        }

        public float End
        {
            get { return end; }
            set { end = value > 1 ? 1 : value; }
        }


        public bool isEmpty()
        {
            return (Start == 1) && (End == 0);
        }
    }
}
