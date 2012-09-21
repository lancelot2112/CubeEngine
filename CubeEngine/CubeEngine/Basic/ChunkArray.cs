using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CubeEngine.Basic
{
    public class ChunkStorage
    {
        public Chunk[, ,] Chunks;

        private int m_xLength;
        private int m_yLength;
        private int m_zLength;

        public ChunkStorage(int xLength, int yLength, int zLength)
        {
            Chunks = new Chunk[xLength, yLength, zLength];

            m_xLength = xLength;
            m_yLength = yLength;
            m_zLength = zLength;
        }

        public Chunk GetXGreater(int x, int y, int z)
        {
            int newX = x + 1;
            if (newX >= m_xLength) newX -= m_xLength;
            return Chunks[newX, y, z];
        }

        public Chunk GetXLesser(int x, int y, int z)
        {
            int newX = x - 1;
            if (newX < 0) newX += m_xLength;
            return Chunks[newX, y, z];
        }

        public Chunk GetYGreater(int x, int y, int z)
        {
            int newY = y + 1;
            if (newY >= m_yLength) newY -= m_yLength;
            return Chunks[x, newY, z];
        }

        public Chunk GetYLesser(int x, int y, int z)
        {
            int newY = y - 1;
            if (newY < 0) newY += m_yLength;
            return Chunks[x, newY, z];
        }

        public Chunk GetZGreater(int x, int y, int z)
        {
            int newZ = z + 1;
            if (newZ >= m_zLength) newZ -= m_zLength;
            return Chunks[x, y, newZ];
        }

        public Chunk GetZLesser(int x, int y, int z)
        {
            int newZ = z - 1;
            if (newZ < 0) newZ += m_zLength;
            return Chunks[x, y, newZ];
        }
    }
}
