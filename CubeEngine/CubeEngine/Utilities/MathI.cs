using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CubeEngine.Utilities
{
    public class MathI
    {
        public static bool IsPowerOf2(int val)
        {
            return (val != 0) && ((val & (val - 1)) == 0);
        }
    }
}
