using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CubeEngine.Basic
{
    // Types of cube available
    public enum CubeType : ushort
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
        Water,
        MAXIMUM
    }

    [Flags]
    public enum CubeFace : byte
    {
        NONE = 0,
        PosX = 1,
        NegX = 2,
        PosY = 4,
        NegY = 8,
        PosZ = 16,
        NegZ = 32
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

        public bool IsTransparent()
        {
            if (Type == CubeType.Air ||
                Type == CubeType.Water ||
                Type == CubeType.NULL)
            {
                return true;
            }
            else return false;
        }

        public bool IsRenderable()
        {
            if (Type == CubeType.Air ||
                Type == CubeType.NULL)
            {
                return false;
            }
            else return true;
        }

        //public static Vector3 CORNER_PPP = new Vector3(0.5f, 0.5f, 0.5f);
        //public static Vector3 CORNER_PPN = new Vector3(0.5f, 0.5f, -0.5f);
        //public static Vector3 CORNER_PNP = new Vector3(0.5f, -0.5f, 0.5f);
        //public static Vector3 CORNER_PNN = new Vector3(0.5f, -0.5f, -0.5f);
        //public static Vector3 CORNER_NPP = new Vector3(-0.5f, 0.5f, 0.5f);
        //public static Vector3 CORNER_NPN = new Vector3(-0.5f, 0.5f, -0.5f);
        //public static Vector3 CORNER_NNP = new Vector3(-0.5f, -0.5f, 0.5f);
        //public static Vector3 CORNER_NNN = new Vector3(-0.5f, -0.5f, -0.5f);

        public static Vector3 CORNER_PPP = new Vector3(1f, 1f, 1f);
        public static Vector3 CORNER_PPN = new Vector3(1f, 1f, 0f);
        public static Vector3 CORNER_PNP = new Vector3(1f, 0f, 1f);
        public static Vector3 CORNER_PNN = new Vector3(1f, 0f, 0f);
        public static Vector3 CORNER_NPP = new Vector3(0f, 1f, 1f);
        public static Vector3 CORNER_NPN = new Vector3(0f, 1f, 0f);
        public static Vector3 CORNER_NNP = new Vector3(0f, 0f, 1f);
        public static Vector3 CORNER_NNN = new Vector3(0f, 0f, 0f);

    }
}
