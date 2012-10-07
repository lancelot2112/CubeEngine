using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace CubeEngine.Basic
{

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

    public enum CubeType : byte
    {
        NULL = 0,
        Air,
        Dirt,
        Grass,
        Stone
    }
    // Value type defining the characteristics of a cube
    public struct Cube
    {
        public byte Red, Green, Blue, AlphaSpecular;
        public byte LocalRed, LocalGreen, LocalBlue, LightLevels;
        public CubeType Type;
        public byte Other;

        public Cube(CubeType type)
        {
            this = CUBE_TYPES[(byte)type];
            this.Red = (byte)((XerUtilities.Common.MathLib.NextRandom() * 20) + Red);
            this.Blue = (byte)((XerUtilities.Common.MathLib.NextRandom() * 20) + Blue);
            this.Green = (byte)((XerUtilities.Common.MathLib.NextRandom() * 20) + Green);
        }

        public Cube(CubeType type, byte red, byte green, byte blue, byte alphaSpecular)
            :this(type,red,green, blue,alphaSpecular,0,0,0,0,0) {}

        public Cube(CubeType type, byte red, byte green, byte blue, byte alphaSpecular, byte localRed, byte localGreen, byte localBlue, byte lightLevels, byte other)
        {
            this.Type = type;
            this.Red = red;
            this.Blue = blue;
            this.Green = green;
            this.AlphaSpecular = alphaSpecular;
            this.LocalRed = localRed;
            this.LocalBlue = localBlue;
            this.LocalGreen = localGreen;
            this.LightLevels = lightLevels;
            this.Other = other;
        }

        public int Opacity
        {
            get
            {
                return AlphaSpecular & 15;
            }
        }

        public bool IsTransparent
        {
            get
            {
                if ((AlphaSpecular & 15) != 15) return true;
                else return false;
            }
        }

        public bool IsRenderable
        {
            get
            {
                if ((AlphaSpecular & 15) != 0) return true;
                return false;
            }
        }

        public int SunLight { get { return LightLevels & 15; } set { LightLevels = (byte)((value & 15) | (LightLevels & 240)); } }
        public int LocalLight { get { return (LightLevels & 240) >> 4; } set { LightLevels = (byte)(((value & 15) << 4) | (LightLevels & 15)); } }
        public int Alpha { get { return AlphaSpecular & 15; } set { AlphaSpecular = (byte)((value & 15) | (AlphaSpecular & 240)); } }
        //TODO: Work on fixing set
        public int Specular { get { return (AlphaSpecular & 240) >> 4; } set { AlphaSpecular = (byte)(((value & 15) << 4) | (AlphaSpecular & 15)); } }

        public override string ToString()
        {
            return "t: " + Type.ToString() + " | sl: " + SunLight.ToString() + " | ll: " + LocalLight.ToString();
        }

        public static Cube NULL = new Cube();
        public static Cube[] CUBE_TYPES = new Cube[] 
        { new Cube(CubeType.NULL,0,0,0,0),
            new Cube(CubeType.Air,0,0,0,0),
            new Cube(CubeType.Dirt,193,154,107,15),
            new Cube(CubeType.Grass,0,100,0,15),
            new Cube(CubeType.Stone,112,138,144,15)
        };
    }
}
