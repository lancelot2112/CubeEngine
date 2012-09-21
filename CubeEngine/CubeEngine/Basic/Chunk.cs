using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CubeEngine.Basic
{
    /// <summary>
    /// A collection of cubes stored in a 3d array along with a mesh for rendering the terrain.
    /// </summary>
    public class Chunk
    {
        public const byte SIZE_X = 64;
        public const byte SIZE_Y = 64;
        public const byte SIZE_Z = 64;
 
        private Cube[,,] m_Cubes;
        public SuperGroupCoords Coords;
        public ChunkHash Hash;
        public Matrix WorldMatrix;

        //Render Members
        public bool AllowRender;
        private List<VertexPositionColor> solidVertexList;
        private List<VertexPositionColor> transparentVertexList;
        public VertexBuffer SolidVertexBuffer;
        public VertexBuffer TransparentVertexBuffer;


        public Chunk()
        {
            m_Cubes = new Cube[SIZE_X, SIZE_Y, SIZE_Z];

            for (byte x = 0; x < SIZE_X; x++)
                for (byte y = 0; y < SIZE_Y; y++)
                    for (byte z = 0; z < SIZE_Z; z++)
                    {
                        m_Cubes[x, y, z].Type = CubeType.Dirt;
                    }

            solidVertexList = new List<VertexPositionColor>();
            transparentVertexList = new List<VertexPositionColor>();

            WorldMatrix = Matrix.Identity;
        }

        public void SetCube(byte x, byte y, byte z, ref Cube cube)
        {
            if (InChunk(x, y, z)) m_Cubes[x, y, z] = cube;
        }

        public bool GetCube(byte x, byte y, byte z, out Cube cube)
        {
            if (InChunk(x, y, z))
            {
                cube = m_Cubes[x, y, z];
                return true;
            }
            else
            {
                cube = Cube.NULL;
                return false;
            }
        }

        public Cube GetCube(byte x, byte y, byte z)
        {
            if (InChunk(x, y, z)) return m_Cubes[x, y, z];
            else return Cube.NULL;
        }

        public bool InChunk(byte x, byte y, byte z)
        {

            if (x < 0 || x >= SIZE_X)
                return false;
            if (y < 0 || y >= SIZE_Y)
                return false;
            if (z < 0 || z >= SIZE_Z)
                return false;

            return true;
        }

        public void BuildVertices(GraphicsDevice graphics, Chunk posX = null, Chunk negX = null, Chunk posY = null, Chunk negY = null, Chunk posZ = null, Chunk negZ = null)
        {
            Cube neighbor;
            Cube current;
            List<VertexPositionColor> vertexList;
            Vector3 offset;

            for(byte x = 0; x < SIZE_X; x++)
                for(byte y = 0; y < SIZE_Y; y++)
                    for (byte z = 0; z < SIZE_Z; z++)
                    {
                        current = m_Cubes[x, y, z];
                        if (current.IsTransparent()) vertexList = transparentVertexList;
                        else vertexList = solidVertexList;

                        offset.X = x;
                        offset.Y = y;
                        offset.Z = z;

                        //-x
                        if (x == 0) neighbor = (negX != null) ? negX.GetCube(SIZE_X - 1, y, z) : Cube.NULL;
                        else neighbor = m_Cubes[x - 1, y, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPN + offset, Color.Green));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPP + offset, Color.Green));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNN + offset, Color.Green));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNN + offset, Color.Green));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPP + offset, Color.Green));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNP + offset, Color.Green));
                        }

                        //+x
                        if (x == SIZE_X - 1) neighbor = (posX != null) ? posX.GetCube(0, y, z) : Cube.NULL;
                        else neighbor = m_Cubes[x + 1, y, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPP + offset, Color.Blue));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPN + offset, Color.Blue));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNP + offset, Color.Blue));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNP + offset, Color.Blue));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPN + offset, Color.Blue));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNN + offset, Color.Blue));
                        }

                        //-y
                        if (y == 0) neighbor = (negY != null) ? negY.GetCube(x, SIZE_Y - 1, z) : Cube.NULL;
                        else neighbor = m_Cubes[x, y - 1, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNP + offset, Color.Orange));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNP + offset, Color.Orange));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNN + offset, Color.Orange));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNN + offset, Color.Orange));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNP + offset, Color.Orange));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNN + offset, Color.Orange));
                        }

                        //+y
                        if (y == SIZE_Y - 1) neighbor = (posY != null) ? posY.GetCube(x, 0, z) : Cube.NULL;
                        else neighbor = m_Cubes[x, y + 1, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPN + offset, Color.Yellow));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPN + offset, Color.Yellow));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPP + offset, Color.Yellow));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPP + offset, Color.Yellow));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPN + offset, Color.Yellow));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPP + offset, Color.Yellow));
                        }

                        //-z
                        if (z == 0) neighbor = (negZ != null) ? negZ.GetCube(x, y, SIZE_Z - 1) : Cube.NULL;
                        else neighbor = m_Cubes[x, y, z - 1];

                        if (neighbor.IsTransparent())
                        {                            
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPN + offset, Color.Violet));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPN + offset, Color.Violet));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNN + offset, Color.Violet));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNN + offset, Color.Violet));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPN + offset, Color.Violet));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNN + offset, Color.Violet));
                        }

                        //+z
                        if (z == SIZE_Z - 1) neighbor = (posZ != null) ? posZ.GetCube(x, y, 0) : Cube.NULL;
                        else neighbor = m_Cubes[x, y, z + 1];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPP + offset, Color.Red));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPP + offset, Color.Red));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNP + offset, Color.Red));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNP + offset, Color.Red));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPP + offset, Color.Red));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNP + offset, Color.Red));
                        }
                    }

            if (solidVertexList.Count > 0)
            {
                SolidVertexBuffer = new VertexBuffer(graphics, typeof(VertexPositionColor), solidVertexList.Count, BufferUsage.None);
                SolidVertexBuffer.SetData<VertexPositionColor>(solidVertexList.ToArray());
                solidVertexList.Clear();
            }

            if (transparentVertexList.Count > 0)
            {
                TransparentVertexBuffer = new VertexBuffer(graphics, typeof(VertexPositionColor), solidVertexList.Count, BufferUsage.None);
                TransparentVertexBuffer.SetData<VertexPositionColor>(transparentVertexList.ToArray());
                transparentVertexList.Clear();
            }
        }

        public void Dispose()
        {
            SolidVertexBuffer.Dispose();
            TransparentVertexBuffer.Dispose();
        }
    }

    /// <summary>
    /// ChunkCoords store the relative positions of a supergroup (256x256x256) of chunks to allow for much bigger worlds while using relative coordinates.  Each supergroup has
    /// chunks with x,y,z in the set [0,255] and by taking the difference between the REL coordinates you can determine the spatial relation of the supergroups.  For instance
    /// REL_X1 = 1 and REL_X2 = 1.001, since 1.001-1 equals a positive number equivalent to the value of SHIFT then supergroup 2 is the supergroup just to the right of supergroup
    /// 1.  Doing it this way allows for huge worlds (much bigger than will every be necessary).
    /// </summary>
    public struct SuperGroupCoords
    {
        public static double SHIFT = 1d;
        public double REL_X;
        public double REL_Y;
        public double REL_Z;

        public SuperGroupCoords(double relX, double relY, double relZ)
        {
            this.REL_X = relX;
            this.REL_Y = relY;
            this.REL_Z = relZ;
        }

    }

    /// <summary>
    /// ChunkHash is used to store the relative positions of chunks stored in memory, up to 256x256x256 chunks.
    /// Uses a wraparound coordinate system by using byte math (ie. 256 + 1 = 0 and 0 - 1 = 256)
    /// </summary>
    public struct ChunkHash
    {

        public byte X;
        public byte Y;
        public byte Z;
        public int Hash;
        public ChunkHash(byte x, byte y , byte z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            Hash = GetHash(x, y, z);
        }

        public override string ToString()
        {
            return Hash.ToString();
        }

        public static int GetHash(byte x, byte y, byte z)
        {
            return x + (y << 8) + (z << 16);
        }

        public int Add(byte x, byte y, byte z)
        {
            return Hash + x + (y << 8) + (z << 16);
        }

        public int Sub(byte x, byte y, byte z)
        {
            return Hash - x - (y << 8) - (z << 16);
        }

        public int AddX(byte x)
        {
            return Hash + x;
        }

        public int SubX(byte x)
        {
            return Hash - x;
        }

        public int AddY(byte y)
        {
            return Hash + (y << 8);
        }

        public int SubY(byte y)
        {
            return Hash - (y << 8);
        }

        public int AddZ(byte z)
        {
            return Hash + (z << 16);
        }

        public int SubZ(byte z)
        {
            return Hash - (z << 16);
        }

        public static ChunkHash GetCoords(int hash)
        {
            byte x = (byte)hash;
            byte y = (byte)(hash>>8);
            byte z = (byte)(hash>>16);
            return new ChunkHash(x, y, z);
        }


    }
}
