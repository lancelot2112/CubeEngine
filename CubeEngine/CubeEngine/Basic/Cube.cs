using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CubeEngine.Basic
{
    // Types of cube available
    public enum CubeType : byte
    {
        NULL = 0,
        Air,
        Dirt,
        Grass,
        Rock,
        Snow,
        Sand,
        TreeTrunk,
        Leaves,
        MAXIMUM
    }
    // Value type defining the characteristics of a cube
    public struct Cube
    {
        public CubeType Type;

        public Cube(CubeType cubeType)
        {
            Type = cubeType;
        }

        public readonly static Cube NULL = new Cube();
    }
}
