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

        private List<Chunk> m_activeChunks;
        public int ActiveChunks { get { return m_activeChunks.Count; } }

        private int m_dimLength;
        private float m_invDimLength;

        public ChunkStorage(int radius)
        {
            m_dimLength = 2 * radius + 5;
            m_invDimLength = 1 / (float)m_dimLength;

            Chunks = new Chunk[m_dimLength, m_dimLength];
            m_activeChunks = new List<Chunk>();
        }

        public void Store(Chunk chunk, Queue<Chunk> unloadQueue)
        {
            int x = WrapCoord(chunk.Coords.X);
            int z = WrapCoord(chunk.Coords.Z);

            if (Chunks[x, z] != null)
            {
                unloadQueue.Enqueue(Chunks[x, z]);
            }

            chunk.XIndex = x;
            chunk.ZIndex = z;

            Chunks[x, z] = chunk;

            if (!m_activeChunks.Contains(chunk)) m_activeChunks.Add(chunk);
        }

        public void UpdateChunks(float dt, Vector3 deltaPosition)
        {
            for (int i = 0; i < m_activeChunks.Count; i++)
            {
                m_activeChunks[i].Update(dt, deltaPosition);
            }
        }
        public Chunk GetChunk(int x, int z)
        {
            x = WrapCoord(x);
            z = WrapCoord(z);

            return Chunks[x, z];
        }

        public bool Contains(ref ChunkCoords coords)
        {
            int x = WrapCoord(coords.X);
            int z = WrapCoord(coords.Z);

            return (Chunks[x, z] != null) ? Chunks[x, z].Coords == coords : false;
        }

        public bool Contains(Chunk chunk)
        {
            return m_activeChunks.Contains(chunk);
        }

        public int WrapCoord(int val)
        {
            if (val >= m_dimLength)
            {
                int scale = (int)(val * m_invDimLength+1);
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
