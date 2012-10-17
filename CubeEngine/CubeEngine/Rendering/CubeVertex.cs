using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using CubeEngine.Basic;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace CubeEngine.Rendering
{
    public struct CubeVertex : IVertexType
    {
        public Vector3 VertexPosition;
        public NormalizedByte4 Normal;
        public Vector2 Texture;
        public byte Red, Green, Blue, Alpha;
        public byte LocalRed, LocalGreen, LocalBlue, LocalLight;
        public short SkyLight;


        public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Byte4, VertexElementUsage.Normal, 0),
            new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(24, VertexElementFormat.Byte4, VertexElementUsage.Color, 0),
            new VertexElement(28, VertexElementFormat.Byte4, VertexElementUsage.Color, 1),
            new VertexElement(32, VertexElementFormat.Short2, VertexElementUsage.Color, 2)
            );

        public CubeVertex(ref Vector3 position, ref NormalizedByte4 normal, ref Vector2 texture, ref Cube cube, ref Cube n1, ref Cube n2, ref Cube n3, ref Cube n4)
        {
            this.VertexPosition = position;
            this.Normal = normal;
            this.Texture = texture;
            this.Red = cube.Red;
            this.Green = cube.Green;
            this.Blue = cube.Blue;
            this.Alpha = (byte)cube.Alpha;
            int lr = 0;
            int lg = 0;
            int lb = 0;
            if (n1.IsTransparent)
            {
                lr = n1.Red;
                lg = n1.Green;
                lb = n1.Blue;
            }
            if (n2.IsTransparent)
            {
                lr = lr + n2.Red;
                lg = lg + n2.Green;
                lb = lb + n2.Blue;
            }
            if (n3.IsTransparent)
            {
                lr = lr + n3.Red;
                lg = lg + n3.Green;
                lb = lb + n3.Blue;
            }
            if (n4.IsTransparent)
            {
                lr = lr + n4.Red;
                lg = lg + n4.Green;
                lb = lb + n4.Blue;
            }
            this.LocalRed = (byte)(lr >> 2);
            this.LocalGreen = (byte)(lg >> 2);
            this.LocalBlue = (byte)(lb >> 2);
            byte light = (byte)((n1.LightLevels + n2.LightLevels + n3.LightLevels + n4.LightLevels) >> 2);
            this.LocalLight = (byte)((light & 240) >> 4);
            this.SkyLight = (short)(light & 15);
        }

        VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }

        public static Vector3 CORNER_PPP = new Vector3(1f, 1f, 1f);
        public static Vector3 CORNER_PPN = new Vector3(1f, 1f, 0f);
        public static Vector3 CORNER_PNP = new Vector3(1f, 0f, 1f);
        public static Vector3 CORNER_PNN = new Vector3(1f, 0f, 0f);
        public static Vector3 CORNER_NPP = new Vector3(0f, 1f, 1f);
        public static Vector3 CORNER_NPN = new Vector3(0f, 1f, 0f);
        public static Vector3 CORNER_NNP = new Vector3(0f, 0f, 1f);
        public static Vector3 CORNER_NNN = new Vector3(0f, 0f, 0f);

        //public static Vector3 CORNER_PPP = new Vector3(1.0001f, 1.0001f, 1.0001f);
        //public static Vector3 CORNER_PPN = new Vector3(1.0001f, 1.0001f, -0.0001f);
        //public static Vector3 CORNER_PNP = new Vector3(1.0001f, -0.0001f, 1.0001f);
        //public static Vector3 CORNER_PNN = new Vector3(1.0001f, -0.0001f, -0.0001f);
        //public static Vector3 CORNER_NPP = new Vector3(-0.0001f, 1.0001f, 1.0001f);
        //public static Vector3 CORNER_NPN = new Vector3(-0.0001f, 1.0001f, -0.0001f);
        //public static Vector3 CORNER_NNP = new Vector3(-0.0001f, -0.0001f, 1.0001f);
        //public static Vector3 CORNER_NNN = new Vector3(-0.0001f, -0.0001f, -0.0001f);

        public static Vector2 TC00 = new Vector2(0, 0);
        public static Vector2 TC01 = new Vector2(0, 1);
        public static Vector2 TC10 = new Vector2(1, 0);
        public static Vector2 TC11 = new Vector2(1, 1);

        public static NormalizedByte4 N_POS_X = new NormalizedByte4(1, 0, 0, 0);
        public static NormalizedByte4 N_NEG_X = new NormalizedByte4(-1, 0, 0, 0);
        public static NormalizedByte4 N_POS_Y = new NormalizedByte4(0, 1, 0, 0);
        public static NormalizedByte4 N_NEG_Y = new NormalizedByte4(0, -1, 0, 0);
        public static NormalizedByte4 N_POS_Z = new NormalizedByte4(0, 0, 1, 0);
        public static NormalizedByte4 N_NEG_Z = new NormalizedByte4(0, 0, -1, 0);
    }
}
