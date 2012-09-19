using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        private Dictionary<int,Chunk> m_Chunks;
        public List<Chunk> ChunksLoading;
        public List<Chunk> ChunksBuilding;
        public List<Chunk> ChunksRebuilding;
        public List<Chunk> ChunksUnloading;
        public List<Chunk> ChunksVisible;
        public List<Chunk> ChunksCulled;

        /// <summary>
        /// Configurable settings to set how far to load blocks.
        /// </summary>
        static int xBlocksLoadRadius = 32;
        static int yBlocksLoadRadius = 32;
        static int zBlocksLoadRadius = 32;
        static int xChunksLoadRadius = xBlocksLoadRadius / Chunk.CHUNK_SIZE;
        static int yChunksLoadRadius = yBlocksLoadRadius / Chunk.CHUNK_SIZE;
        static int zChunksLoadRadius = zBlocksLoadRadius / Chunk.CHUNK_SIZE;
        static int xChunksBuildRadius = xChunksLoadRadius - 1;
        static int yChunksBuildRadius = yChunksLoadRadius - 1;
        static int zChunksBuildRadius = zChunksLoadRadius - 1;



        public ChunkManager()
        {
            m_Chunks = new Dictionary<int,Chunk>();
            ChunksLoading = new List<Chunk>();
            ChunksBuilding = new List<Chunk>();
            ChunksRebuilding = new List<Chunk>();
            ChunksUnloading = new List<Chunk>();
            ChunksVisible = new List<Chunk>();
            ChunksCulled = new List<Chunk>();
        }

        public void Update(float dt)
        {
            LoadChunks();
            BuildChunkVertices();
            RebuildChunkVertices();
            UnloadChunks();
        }

        public void LoadChunks()
        {
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

        public void UnloadChunks()
        {
        }
    }
}
