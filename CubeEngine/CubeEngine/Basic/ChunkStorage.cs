using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CubeEngine.Basic
{
    public class ChunkStorage
    {
        public Chunk[,] Chunks;

        public List<Chunk> LoadedChunks;
        public int LoadedChunkCount { get { return LoadedChunks.Count; } }

        private int m_dimLength;
        private float m_invDimLength;

        public ChunkStorage(int radius)
        {
            m_dimLength = 2 * radius + 2;
            m_invDimLength = 1 / (float)m_dimLength;

            Chunks = new Chunk[m_dimLength, m_dimLength];
            LoadedChunks = new List<Chunk>();
        }

        public void Store(Chunk chunk, ChunkManager manager)
        {
            int x = WrapCoord(chunk.Coords.X);
            int z = WrapCoord(chunk.Coords.Z);

            if (Chunks[x, z] != null)
            {               
                LoadedChunks.Remove(Chunks[x, z]);
                manager.PrepareChunkForUnload(Chunks[x, z]);
            }

            chunk.XIndex = x;
            chunk.ZIndex = z;

            Chunks[x, z] = chunk;

            LoadedChunks.Add(chunk);
        }

        public void UpdateChunks(float dt, Vector3 deltaPosition)
        {
            //TODO: Make sure entire list gets updated upon removal from an off thread
            for (int i = 0; i < LoadedChunks.Count; i++)
            {
                LoadedChunks[i].Update(dt, deltaPosition);
            }
        }
        public Chunk GetChunk(int x, int z)
        {
            int X = x;
            int Z = z;
            
            x = WrapCoord(x);
            z = WrapCoord(z);

            return Chunks[x, z];
        }

        public bool Contains(ref ChunkCoords coords)
        {
            int x = WrapCoord(coords.X);
            int z = WrapCoord(coords.Z);

            return (Chunks[x, z] != null) ? Chunks[x, z].Coords.Equals(ref coords) : false;
        }

        public bool Contains(Chunk chunk)
        {
            return LoadedChunks.Contains(chunk);
        }

        public int WrapCoord(int val)
        {
            if (val >= m_dimLength)
            {
                int scale = (int)(val * m_invDimLength);
                val -= m_dimLength * scale;
            }
            else if (val < 0)
            {
                int scale = (int)(-val * m_invDimLength+1);
                val += m_dimLength * scale;
            }

            return val;

        }

        
    }
}
