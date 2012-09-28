using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        private GraphicsDevice m_graphics;

        /// <summary>
        /// Configurable settings to set how far to load blocks.
        /// </summary>

        public static int chunkRadius = 5;
        public const int PER_FRAME_CHUNKS_LOAD = 1;
        public const int PER_FRAME_CHUNKS_BUILD = 1;
        public const float TIME_BETWEEN_LOADS = 1f;
        public const float TIME_BETWEEN_BUILDS = 1f;
        public const int CHUNK_BUILD_DIST = 5;

        //public static int xOffset = (int)(xChunkNumber * 0.5f);
        //public static int yOffset = (int)(yChunkNumber * 0.5f);
        //public static int zOffset = (int)(zChunkNumber * 0.5f);

        public delegate void ChunkLoadedHandler(ChunkManager manager, Chunk coords);
        public event ChunkLoadedHandler ChunkLoadedEvent;

        public List<Chunk> UnbuiltChunks;
        private bool m_unbuiltChunksSortNeeded;

        private volatile bool loadDone;
        private volatile bool buildDone;
        private volatile bool unloadDone;
        private ManualResetEvent cullDone;

        private ChunkStorage m_chunkStorage;
        private Queue<ChunkCoords> m_loadQueue;        
        public Queue<Chunk> ChunksToBuild;
        public Queue<Chunk> ChunksToRebuild;
        public Queue<Chunk> ChunksToUnload;
        public List<Chunk> BuiltChunks;
        public List<Chunk> ChunksToDraw;

        private ChunkCoords m_previousPlayerPosition;

        private float m_timeSinceBuild;
        private float m_timeSinceLoad;
        private List<VertexPositionColor> m_vertexBuffer;

        public ChunkManager(GraphicsDevice graphics, ChunkCoords playerPosition)
        {
            m_graphics = graphics;

            UnbuiltChunks = new List<Chunk>();
            m_unbuiltChunksSortNeeded = false;

            m_chunkStorage = new ChunkStorage(chunkRadius);
            m_loadQueue = new Queue<ChunkCoords>();
            m_vertexBuffer = new List<VertexPositionColor>();
            
            ChunksToBuild = new Queue<Chunk>();
            ChunksToRebuild = new Queue<Chunk>();
            ChunksToUnload = new Queue<Chunk>();
            ChunksToDraw = new List<Chunk>();

            loadDone = true;
            buildDone = true;
            unloadDone = true;

            m_loadQueue.Enqueue(playerPosition);
        }

        public void Update(float dt, ChunkCoords playerPosition, Vector3 deltaPosition)
        {
            QueueChunks(playerPosition);

            m_timeSinceLoad += dt;
            if (m_timeSinceLoad > TIME_BETWEEN_LOADS)
            {
                m_timeSinceLoad -= TIME_BETWEEN_LOADS;
                if (m_loadQueue.Count > 0 && loadDone)
                {
                    loadDone = false;
                    ThreadPool.QueueUserWorkItem(LoadChunks);
                }
            }

            m_timeSinceBuild += dt;
            if (m_timeSinceBuild > TIME_BETWEEN_BUILDS)
            {
                m_timeSinceBuild -= TIME_BETWEEN_BUILDS;
                if (ChunksToBuild.Count > 0 && buildDone)
                {
                    buildDone = false;
                    ThreadPool.QueueUserWorkItem(BuildChunkVertices);
                }
            }
            RebuildChunkVertices();

            if (ChunksToUnload.Count > 0 && unloadDone)
            {
                unloadDone = false;
                ThreadPool.QueueUserWorkItem(UnloadChunks,false);
            }

            m_chunkStorage.UpdateChunks(dt, deltaPosition);

            m_previousPlayerPosition = playerPosition;
        }

        public void QueueChunks(ChunkCoords playerPosition)
        {

            if (m_unbuiltChunksSortNeeded) UnbuiltChunks.Sort((x, y) => x.Coords.CompareDistance(y.Coords, playerPosition));

            Chunk curr;
            ChunkCoords coords;
            for (int i = 0; i < UnbuiltChunks.Count; i++)
            {
                curr = UnbuiltChunks[i];
                if (curr.Coords.Distance(ref playerPosition) <= CHUNK_BUILD_DIST)
                {
                    if (curr.DependenciesFlag == Chunk.DEPENDENCIES_MET_FLAG_VALUE)
                    {
                        ChunksToBuild.Enqueue(curr);
                        UnbuiltChunks.Remove(curr);
                    }
                    else
                    {
                        
                        if ((curr.DependenciesFlag & 1) != 1)
                        {
                            coords = curr.Coords.GetShiftedX(1);
                            if (!m_chunkStorage.Contains(ref coords))
                            {
                                if (!m_loadQueue.Contains(coords)) m_loadQueue.Enqueue(coords);
                            }
                            else
                            {
                                curr.DependenciesFlag |= 1;
                            }
                        }

                        if ((curr.DependenciesFlag & 2) != 2)
                        {
                            coords = curr.Coords.GetShiftedX(-1);
                            if (!m_chunkStorage.Contains(ref coords))
                            {
                                if (!m_loadQueue.Contains(coords)) m_loadQueue.Enqueue(coords);
                            }
                            else
                            {
                                curr.DependenciesFlag |= 2;
                            }
                        }

                        if ((curr.DependenciesFlag & 4) != 4)
                        {
                            coords = curr.Coords.GetShiftedZ(1);
                            if (!m_chunkStorage.Contains(ref coords))
                            {
                                if (!m_loadQueue.Contains(coords)) m_loadQueue.Enqueue(coords);
                            }
                            else
                            {
                                curr.DependenciesFlag |= 4;
                            }
                        }

                        if ((curr.DependenciesFlag & 8) != 8)
                        {
                            coords = curr.Coords.GetShiftedZ(-1);
                            if (!m_chunkStorage.Contains(ref coords))
                            {
                                if (!m_loadQueue.Contains(coords)) m_loadQueue.Enqueue(coords);
                            }
                            else
                            {
                                curr.DependenciesFlag |= 8;
                            }
                        }
                    }
                }
                else break;
            }


        }

        public void LoadChunks(Object threadContext)
        {
            int loadedThisFrame = 0;
            Chunk curr;
            Cube dirt = new Cube(CubeType.Dirt);
            Cube air = new Cube(CubeType.Air);

            while(m_loadQueue.Count>0 && (PER_FRAME_CHUNKS_LOAD-loadedThisFrame) >0)
            {
                curr = new Chunk(this, m_loadQueue.Dequeue());
                if (!curr.LoadFromDisk())
                {
                    //Generate using terrain generation
                    for (int x = 0; x < Chunk.WIDTH; x++)
                        for (int y = 0; y < Chunk.HEIGHT; y++)
                            for (int z = 0; z < Chunk.WIDTH; z++)
                            {
                                if (XerUtilities.Common.MathLib.NextRandom() > 0.5f)
                                {
                                    curr.SetCube(x, y, z, ref dirt);
                                }
                                else curr.SetCube(x, y, z, ref air);
                            }
                }

                m_chunkStorage.Store(curr, ChunksToUnload);
                UnbuiltChunks.Add(curr);
                m_unbuiltChunksSortNeeded = true;
                ChunkLoadedEvent(this, curr);
                loadedThisFrame += 1;                
            }
            loadDone = true;
        }

        public void BuildChunkVertices(Object threadContext)
        {
            int chunksBuiltThisFrame = 0;

            Chunk curr;
            Chunk posX = null;
            Chunk negX = null;
            Chunk posY = null;
            Chunk negY = null;
            while (ChunksToBuild.Count > 0 && PER_FRAME_CHUNKS_BUILD - chunksBuiltThisFrame >= 0)
            {
                curr = ChunksToBuild.Dequeue();
                negX = m_chunkStorage.GetChunk(curr.Coords.X - 1, curr.Coords.Z);
                posX = m_chunkStorage.GetChunk(curr.Coords.X + 1, curr.Coords.Z);
                negY = m_chunkStorage.GetChunk(curr.Coords.X, curr.Coords.Z - 1);
                posY = m_chunkStorage.GetChunk(curr.Coords.X, curr.Coords.Z + 1);

                curr.BuildVertices(m_vertexBuffer, m_graphics, posX, negX, posY, negY);

                if(curr.Meshes.Count > 0 && !ChunksToDraw.Contains(curr)) ChunksToDraw.Add(curr);

                chunksBuiltThisFrame += 1;
            }

            buildDone = true;
        }

        public void RebuildChunkVertices()
        {
        }

        public void GetVisibleChunks(Object threadContext)
        {
        }

        public void CullChunks()
        {
        }

        public void UnloadChunks(Object threadContext)
        {
            bool closing = (bool)threadContext;
            if (!closing)
            {
		        Chunk temp;
                for (int i = 0; i < ChunksToUnload.Count; i++)
                {

                    temp = ChunksToUnload.Dequeue();
                    if(temp.ChangedSinceLoad) temp.SaveToDisk();
                    temp.Dispose();
                }
            }

            unloadDone = true;
        }
    }
}
