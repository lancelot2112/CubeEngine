using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

namespace CubeEngine.Basic
{
    /// <summary>
    /// A collection of cubes stored in a 3d array along with a mesh for rendering the terrain.
    /// </summary>
    public class Chunk
    {
        public const byte CHUNK_SIZE = 16;
 
        private Cube[,,] m_Cubes;
        public SuperGroupCoords Coords;
        public ChunkHash Hash;
        public Matrix WorldMatrix;
        public bool Render;

        public Chunk()
        {
            m_Cubes = new Cube[CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE];
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

        public bool InChunk(byte x, byte y, byte z)
        {

            if (x < 0 || x >= CHUNK_SIZE)
                return false;
            if (y < 0 || y >= CHUNK_SIZE)
                return false;
            if (z < 0 || z >= CHUNK_SIZE)
                return false;

            return true;
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

        public static ChunkHash GetCoords(int hash)
        {
            byte x = (byte)hash;
            byte y = (byte)(hash>>8);
            byte z = (byte)(hash>>16);
            return new ChunkHash(x, y, z);
        }
    }
}
