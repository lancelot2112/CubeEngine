using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace CubeEngine.Basic
{
    /// <summary>
    /// Chunk Manager general flow
    /// 1. ASYNC Check if any chunks need to be loaded (from disk or into memory)
    /// 2. ASYNC Check if newly loaded chunks need to be generated (ie. procedurally generated)
    /// 3. SEQ   Check for any chunks that needs mesh (re)built
    /// 4. ASYNC Check if any chunks need to be unloaded
    /// 5. SEQ Update chunk visibility list (list of all chunks that could potentially be rendered, ie. within view distance)
    /// 6. SEQ Update render list (perform frustum culling, occlusion culling remove empty chunks)
    /// </summary>
    public class ChunkManager
    {
        private GraphicsDevice m_Graphics;

        /// <summary>
        /// Configurable settings to set how far to load blocks.
        /// </summary>
        static int xBlocksLoadRadius = 256;
        static int yBlocksLoadRadius = 128;
        static int zBlocksLoadRadius = 256;
        static int xChunksLoadRadius = xBlocksLoadRadius / Chunk.SIZE_X;
        static int yChunksLoadRadius = yBlocksLoadRadius / Chunk.SIZE_Y;
        static int zChunksLoadRadius = zBlocksLoadRadius / Chunk.SIZE_Z;
        static int xChunksBuildRadius = xChunksLoadRadius - 1;
        static int yChunksBuildRadius = yChunksLoadRadius - 1;
        static int zChunksBuildRadius = zChunksLoadRadius - 1;

        private Dictionary<int, Chunk> m_Chunks;
        private Chunk[, ,] m_ChunksArray;
        public List<Chunk> ChunksBuilding;
        public List<Chunk> ChunksRebuilding;
        public List<Chunk> ChunksUnloading;
        public List<Chunk> ChunksToCull;
        public List<Chunk> ChunksVisible;

        public ChunkManager(GraphicsDevice graphics)
        {
            m_Graphics = graphics;

            m_Chunks = new Dictionary<int,Chunk>();
            m_ChunksArray = new Chunk[xChunksLoadRadius * 2 + 1, yChunksLoadRadius * 2 + 1, zChunksLoadRadius * 2 + 1];
            ChunksBuilding = new List<Chunk>();
            ChunksRebuilding = new List<Chunk>();
            ChunksUnloading = new List<Chunk>();
            ChunksToCull = new List<Chunk>();
            ChunksVisible = new List<Chunk>();
            LoadChunks();
        }

        public void Update(float dt)
        {
            //LoadChunks();
            BuildChunkVertices();
            RebuildChunkVertices();
            UnloadChunks(false);
        }

        public void LoadChunks()
        {
            float offsetX;
            float offsetY;
            float offsetZ;
            Chunk newChunk;
            for (byte x = 0; x < xChunksLoadRadius; x++)
            {
                offsetX = x * Chunk.SIZE_X;
                for (byte y = 0; y < yChunksLoadRadius; y++)
                {
                    offsetY = y * Chunk.SIZE_Y;
                    for (byte z = 0; z < zChunksLoadRadius; z++)
                    {
                        offsetZ = z * Chunk.SIZE_Z;
                        newChunk = new Chunk();
                        Matrix.CreateTranslation(offsetX, offsetY, offsetZ, out newChunk.WorldMatrix);
                        newChunk.BuildVertices(m_Graphics);
                        ChunksVisible.Add(newChunk);
                        if (m_ChunksArray[x, y, z] != null) ChunksUnloading.Add(m_ChunksArray[x, y, z]);
                        m_ChunksArray[x, y, z] = newChunk;
                    }
                }
            }
        }

        public void BuildChunkVertices()
        {
        }

        public void RebuildChunkVertices()
        {
        }

        public void CheckChunkVisibility()
        {
        }

        public void CullChunks()
        {
        }

        public void UnloadChunks(bool closing)
        {
            if (!closing)
            {
                for (int i = 0; i < ChunksUnloading.Count; i++)
                {
                    ChunksUnloading[i].Dispose();
                }
            }
        }

    }
}
