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
        //public Byte4 Texture;
        //public Byte4 ColorInformation;
        //public Byte4 LightInformation;
        public byte Red, Green, Blue, Alpha;
        public byte LocalRed, LocalGreen, LocalBlue, LocalLight;
        public short Luminance, SkyLight;


        public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Byte4, VertexElementUsage.Normal, 0),
            new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(24, VertexElementFormat.Byte4, VertexElementUsage.Color, 0),
            new VertexElement(28, VertexElementFormat.Byte4, VertexElementUsage.Color, 1),
            new VertexElement(32, VertexElementFormat.Short2, VertexElementUsage.Color, 2)
            );

        public CubeVertex(ref Vector3 position, ref NormalizedByte4 normal, ref Vector2 texture, ref Cube cube, ref Cube neighbor)
        {
            this.VertexPosition = position;
            this.Normal = normal;
            this.Texture = texture;
            this.Red = cube.Red;
            this.Green = cube.Green;
            this.Blue = cube.Blue;
            this.Alpha = (byte)cube.Alpha;
            this.Luminance = (byte)cube.Specular;
            this.LocalRed = neighbor.LocalRed;
            this.LocalGreen = neighbor.LocalGreen;
            this.LocalBlue = neighbor.LocalBlue;
            this.LocalLight = (byte)neighbor.LocalLight;
            this.SkyLight = (byte)neighbor.SunLight;


            //this.Texture = new Byte4(texture.X, texture.Y, neighbor.SunLight, cube.Specular);
            //this.ColorInformation = new Byte4(cube.Red, cube.Green, cube.Blue, cube.Alpha);
            //this.LightInformation = new Byte4(neighbor.LocalRed, neighbor.LocalGreen, neighbor.LocalBlue, neighbor.LocalLight);
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
