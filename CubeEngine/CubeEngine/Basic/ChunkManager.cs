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
        //public static int xBlocksLoadRadius = 0;
        //public static int yBlocksLoadRadius = 0;
        //public static int zBlocksLoadRadius = 0;
        //public static int xChunksLoadRadius = xBlocksLoadRadius / Chunk.SIZE_X;
        //public static int yChunksLoadRadius = yBlocksLoadRadius / Chunk.SIZE_Y;
        //public static int zChunksLoadRadius = zBlocksLoadRadius / Chunk.SIZE_Z;
        //public static int xChunkNumber = 2 * xChunksLoadRadius + 1;
        //public static int yChunkNumber = 2 * yChunksLoadRadius + 1;
        //public static int zChunkNumber = 2 * zChunksLoadRadius + 1;
        //public static int xChunksBuildRadius = xChunksLoadRadius - 1;
        //public static int yChunksBuildRadius = yChunksLoadRadius - 1;
        //public static int zChunksBuildRadius = zChunksLoadRadius - 1;

        public static int xChunkNumber = 3;
        public static int yChunkNumber = 1;
        public static int zChunkNumber = 3;
        public static int xOffset = (int)(xChunkNumber * 0.5f);
        public static int yOffset = (int)(yChunkNumber * 0.5f);
        public static int zOffset = (int)(zChunkNumber * 0.5f);

        private Dictionary<int, Chunk> m_Chunks;
        private ChunkStorage m_ChunkStorage;
        public List<Chunk> ChunksBuilding;
        public List<Chunk> ChunksRebuilding;
        public List<Chunk> ChunksUnloading;
        public List<Chunk> ChunksToCull;
        public List<Chunk> ChunksVisible;

        public ChunkManager(GraphicsDevice graphics)
        {
            m_Graphics = graphics;

            m_Chunks = new Dictionary<int,Chunk>();
            m_ChunkStorage = new ChunkStorage(xChunkNumber, yChunkNumber, zChunkNumber);
            ChunksBuilding = new List<Chunk>();
            ChunksRebuilding = new List<Chunk>();
            ChunksUnloading = new List<Chunk>();
            ChunksToCull = new List<Chunk>();
            ChunksVisible = new List<Chunk>();

            LoadChunks();
            BuildChunkVertices();
        }

        public void Update(float dt)
        {
            //LoadChunks();
            //BuildChunkVertices();
            RebuildChunkVertices();
            UnloadChunks(false);
        }

        public void LoadChunks()
        {
            float offsetX;
            float offsetY;
            float offsetZ;
            Chunk newChunk;
            for (int x = 0; x < xChunkNumber; x++)
            {
                offsetX = (x - xOffset) * Chunk.SIZE_X;
                for (int y = 0; y < yChunkNumber; y++)
                {
                    offsetY = (y - yOffset) * Chunk.SIZE_Y;
                    for (int z = 0; z < zChunkNumber; z++)
                    {
                        offsetZ = (z - zOffset) * Chunk.SIZE_Z;
                        newChunk = new Chunk();
                        newChunk.Index = new ChunkIndex(x, y, z);
                        Matrix.CreateTranslation(offsetX, offsetY, offsetZ, out newChunk.WorldMatrix);
                        if (m_ChunkStorage.Chunks[x, y, z] != null) ChunksUnloading.Add(m_ChunkStorage.Chunks[x, y, z]);
                        m_ChunkStorage.Chunks[x, y, z] = newChunk;
                    }
                }
            }
        }

        public void BuildChunkVertices()
        {
            Chunk curr;
            Chunk posX = null;
            Chunk negX = null;
            Chunk posY = null;
            Chunk negY = null;
            Chunk posZ = null;
            Chunk negZ = null;
            for (int x = 0; x < xChunkNumber; x++)
                for (int y = 0; y < yChunkNumber; y++)
                    for (int z = 0; z < zChunkNumber; z++)
                    {
                        curr = m_ChunkStorage.Chunks[x, y, z];
                        //negX = m_ChunkStorage.GetXLesser(x, y, z);
                        //posX = m_ChunkStorage.GetXGreater(x, y, z);
                        //negY = m_ChunkStorage.GetYLesser(x, y, z);
                        //posY = m_ChunkStorage.GetYGreater(x, y, z);
                        //negZ = m_ChunkStorage.GetZLesser(x, y, z);
                        //posZ = m_ChunkStorage.GetZGreater(x, y, z);

                        curr.BuildVertices(m_Graphics, posX, negX, posY, negY, posZ, negZ);

                        if (curr.SolidVertexBuffer != null || curr.TransparentVertexBuffer != null) ChunksVisible.Add(curr);
                    }
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
