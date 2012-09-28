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
    public class CubeVertex : IVertexType
    {
        public Vector3 VertexPosition;
        public NormalizedByte4 Normal;
        public HalfVector2 Texture;
        public Byte4 ColorInformation;
        public Byte4 LightInformation;
        
        

        public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
            new VertexElement(0,VertexElementFormat.Vector3, VertexElementUsage.Position,0),
            new VertexElement(12,VertexElementFormat.Byte4, VertexElementUsage.Normal,0),
            new VertexElement(24,VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate,0),
            new VertexElement(32,VertexElementFormat.Byte4,VertexElementUsage.Color,0),
            new VertexElement(36,VertexElementFormat.Byte4,VertexElementUsage.Color,1)
            );

        public CubeVertex(ref Vector3 position, ref NormalizedByte4 normal, ref HalfVector2 texture, ref Cube cube)
        {
            this.VertexPosition = position;
            this.Normal = normal;
            this.Texture = texture;
            this.ColorInformation = new Byte4(cube.Red, cube.Green, cube.Blue, cube.AlphaSpecular);
            this.LightInformation = new Byte4(cube.LocalRed, cube.LocalGreen, cube.LocalBlue, cube.LightLevels);
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

        public static HalfVector2 TC00 = new HalfVector2(0, 0);
        public static HalfVector2 TC01 = new HalfVector2(0, 1);
        public static HalfVector2 TC10 = new HalfVector2(1, 0);
        public static HalfVector2 TC11 = new HalfVector2(1, 1);

        public static NormalizedByte4 N_POS_X = new NormalizedByte4(1, 0, 0, 0);
        public static NormalizedByte4 N_NEG_X = new NormalizedByte4(-1, 0, 0, 0);
        public static NormalizedByte4 N_POS_Y = new NormalizedByte4(0, 1, 0, 0);
        public static NormalizedByte4 N_NEG_Y = new NormalizedByte4(0, -1, 0, 0);
        public static NormalizedByte4 N_POS_Z = new NormalizedByte4(0, 0, 1, 0);
        public static NormalizedByte4 N_NEG_Z = new NormalizedByte4(0, 0, -1, 0);
    }
}
