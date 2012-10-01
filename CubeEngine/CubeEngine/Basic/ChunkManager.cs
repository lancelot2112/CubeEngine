using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using CubeEngine.Rendering;

namespace CubeEngine.Basic
{
    using log = XerUtilities.Debugging.Logger;
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

        public const int PER_FRAME_CHUNKS_LOAD = 5;
        public const int PER_FRAME_CHUNKS_LIGHT = 5;
        public const int PER_FRAME_CHUNKS_BUILD = 5;
        public const float TIME_BETWEEN_LOADS = .8f;
        public const float TIME_BETWEEN_LIGHTS = .8f;
        public const float TIME_BETWEEN_BUILDS = .8f;
        public const int CHUNK_BUILD_DIST = 6;
        public const int CHUNK_LOAD_DIST = CHUNK_BUILD_DIST + 2;

        //public static int xOffset = (int)(xChunkNumber * 0.5f);
        //public static int yOffset = (int)(yChunkNumber * 0.5f);
        //public static int zOffset = (int)(zChunkNumber * 0.5f);

        public delegate void ChunkHandler(ChunkManager manager, Chunk chunk);
        public event ChunkHandler ChunkLoadingEvent;
        public event ChunkHandler ChunkLoadedEvent;
        public event ChunkHandler ChunkLitEvent;

        public List<Chunk> AwaitingLightDependenciesList;
        private bool m_awaitingLightSortNeeded;
        public List<Chunk> AwaitingBuildDependenciesList;
        private bool m_awaitingBuildSortNeeded;


        private volatile bool loadDone;
        private volatile bool buildDone;
        private volatile bool unloadDone;
        private volatile bool lightDone;
        //private ManualResetEvent cullDone;

        private ChunkStorage m_chunkStorage;
        private Queue<Chunk> m_loadQueue;
        public Queue<Chunk> LightQueue;
        public Queue<Chunk> BuildQueue;
        public Queue<Chunk> RebuildQueue;
        public Queue<Chunk> UnloadQueue;
        public List<Chunk> DrawList;

        private ChunkCoords m_previousPlayerPosition;

        private float m_timeSinceLoad;
        private float m_timeSinceLight;
        private float m_timeSinceBuild;

        //TODO: Switch to array and figure out how to be able to use it without deallocating.
        private List<CubeVertex> m_vertexBuffer;

        public int LoadedChunkCount { get { return m_chunkStorage.LoadedChunkCount; } }

        public ChunkManager(GraphicsDevice graphics, ChunkCoords playerPosition)
        {
            m_graphics = graphics;

            AwaitingLightDependenciesList = new List<Chunk>();
            AwaitingBuildDependenciesList = new List<Chunk>();
            m_awaitingLightSortNeeded = false;
            m_awaitingBuildSortNeeded = false;

            m_chunkStorage = new ChunkStorage(CHUNK_LOAD_DIST + 1);
            m_loadQueue = new Queue<Chunk>();
            m_vertexBuffer = new List<CubeVertex>();

            LightQueue = new Queue<Chunk>();
            BuildQueue = new Queue<Chunk>();
            RebuildQueue = new Queue<Chunk>();
            UnloadQueue = new Queue<Chunk>();
            DrawList = new List<Chunk>();

            m_timeSinceLoad = 0.8f;
            m_timeSinceLight = 0.4f;
            m_timeSinceBuild = 0.0f;
            loadDone = true;
            buildDone = true;
            unloadDone = true;
            lightDone = true;

            //Initialize chunk loading.
            Chunk seed = new Chunk(this, playerPosition);
            m_loadQueue.Enqueue(seed);
            m_chunkStorage.Store(seed, this);
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
                    ThreadPool.UnsafeQueueUserWorkItem(LoadChunks,0);
                }
            }

            m_timeSinceLight += dt;
            if (m_timeSinceLight > TIME_BETWEEN_LIGHTS)
            {
                m_timeSinceLight -= TIME_BETWEEN_LIGHTS;
                if (LightQueue.Count > 0 && lightDone)
                {
                    lightDone = false;
                    ThreadPool.UnsafeQueueUserWorkItem(LightChunks,0);
                }
            }

            m_timeSinceBuild += dt;
            if (m_timeSinceBuild > TIME_BETWEEN_BUILDS)
            {
                m_timeSinceBuild -= TIME_BETWEEN_BUILDS;
                if (BuildQueue.Count > 0 && buildDone)
                {
                    buildDone = false;
                    ThreadPool.UnsafeQueueUserWorkItem(BuildChunkVertices,0);
                }
            }

            RebuildChunkVertices();

            if (UnloadQueue.Count > 0 && unloadDone)
            {
                unloadDone = false;
                ThreadPool.UnsafeQueueUserWorkItem(UnloadChunks,false);
            }

            m_chunkStorage.UpdateChunks(dt, deltaPosition);

            m_previousPlayerPosition = playerPosition;
        }

        public void QueueChunks(ChunkCoords playerPosition)
        {

            if (m_awaitingLightSortNeeded) AwaitingLightDependenciesList.Sort((x, y) => x.Coords.CompareDistance(ref y.Coords, ref playerPosition));
            if (m_awaitingBuildSortNeeded) AwaitingBuildDependenciesList.Sort((x, y) => x.Coords.CompareDistance(ref y.Coords, ref playerPosition));

            Chunk curr;
            Chunk other;
            ChunkCoords coords;
            for (int i = 0; i < AwaitingLightDependenciesList.Count; i++)
            {
                curr = AwaitingLightDependenciesList[i];
                if (curr.Coords.Distance(ref playerPosition) <= CHUNK_LOAD_DIST)
                {
                    if (curr.LoadDependenciesFlag != Chunk.DEPENDENCIES_MET_FLAG_VALUE)
                    {
                        //Use the chunk awaiting lighting as a seed for loading the blocks around it if
                        //they aren't already loaded.
                        //+x
                        if (((curr.LoadDependenciesFlag & 1) != 1))
                        {
                            curr.Coords.GetShiftedX(1, out coords);
                            if (!m_chunkStorage.Contains(ref coords))
                            {
                                other = new Chunk(this, coords);
                                m_chunkStorage.Store(other, this);
                                m_loadQueue.Enqueue(other);
                                ChunkLoadingEvent(this, other);
                            }
                            else
                            {
                                curr.LoadDependenciesFlag |= 1;
                            }
                        }
                        //-x
                        if (((curr.LoadDependenciesFlag & 2) != 2))
                        {
                            curr.Coords.GetShiftedX(-1, out coords);
                            if (!m_chunkStorage.Contains(ref coords))
                            {
                                other = new Chunk(this, coords);
                                m_chunkStorage.Store(other, this);
                                m_loadQueue.Enqueue(other);
                                ChunkLoadingEvent(this, other);
                            }
                            else
                            {
                                curr.LoadDependenciesFlag |= 2;
                            }
                        }
                        //+z
                        if (((curr.LoadDependenciesFlag & 4) != 4))
                        {
                            curr.Coords.GetShiftedZ(1, out coords);
                            if (!m_chunkStorage.Contains(ref coords))
                            {
                                other = new Chunk(this, coords);
                                m_chunkStorage.Store(other, this);
                                m_loadQueue.Enqueue(other);
                                ChunkLoadingEvent(this, other);
                            }
                            else
                            {
                                curr.LoadDependenciesFlag |= 4;
                            }
                        }
                        //-z
                        if (((curr.LoadDependenciesFlag & 8) != 8))
                        {
                            curr.Coords.GetShiftedZ(-1, out coords);
                            if (!m_chunkStorage.Contains(ref coords))
                            {
                                other = new Chunk(this, coords);
                                m_chunkStorage.Store(other, this);
                                m_loadQueue.Enqueue(other);
                                ChunkLoadingEvent(this, other);
                            }
                            else
                            {
                                curr.LoadDependenciesFlag |= 8;
                            }
                        }
                    }
                    else if (curr.LightDependenciesFlag == Chunk.DEPENDENCIES_MET_FLAG_VALUE)
                    {
                        LightQueue.Enqueue(curr);
                        AwaitingLightDependenciesList.Remove(curr);
                    }
                    else
                    {
                        //+x
                        if (((curr.LightDependenciesFlag & 1) != 1))
                        {
                            curr.Coords.GetShiftedX(1, out coords);
                            other = m_chunkStorage.GetChunk(coords.X, coords.Z);
                            if (other.Loaded == true)
                            {
                                curr.LightDependenciesFlag |= 1;
                            }
                        }
                        //-x
                        if (((curr.LightDependenciesFlag & 2) != 2))
                        {
                            curr.Coords.GetShiftedX(-1, out coords);
                            other = m_chunkStorage.GetChunk(coords.X, coords.Z);
                            if (other.Loaded == true)
                            {
                                curr.LightDependenciesFlag |= 2;
                            }
                        }
                        //+z
                        if (((curr.LightDependenciesFlag & 4) != 4))
                        {
                            curr.Coords.GetShiftedZ(1, out coords);
                            other = m_chunkStorage.GetChunk(coords.X, coords.Z);
                            if (other.Loaded == true)
                            {
                                curr.LightDependenciesFlag |= 4;
                            }
                        }
                        //-z
                        if (((curr.LightDependenciesFlag & 8) != 8))
                        {
                            curr.Coords.GetShiftedZ(-1, out coords);
                            other = m_chunkStorage.GetChunk(coords.X, coords.Z);
                            if (other.Loaded == true)
                            {
                                curr.LightDependenciesFlag |= 8;
                            }
                        }
                        
                    }
                }
                else break;
            }

            for (int i = 0; i < AwaitingBuildDependenciesList.Count; i++)
            {
                curr = AwaitingBuildDependenciesList[i];
                if (curr.Coords.Distance(ref playerPosition) <= CHUNK_BUILD_DIST)
                {
                    if (curr.BuildDependenciesFlag == Chunk.DEPENDENCIES_MET_FLAG_VALUE)
                    {
                        BuildQueue.Enqueue(curr);
                        AwaitingBuildDependenciesList.Remove(curr);
                    }
                }
                else break;
            }
        }

        public void PrintStats()
        {
            Chunk curr;

            log.Write("ManagerStats", "--Light Dependencies--", AwaitingLightDependenciesList.Count.ToString());
            for (int i = 0; i < AwaitingLightDependenciesList.Count; i++)
            {
                curr = AwaitingLightDependenciesList[i];
                log.Write("ManagerStats", curr.ObjectNumber.ToString(), curr.ToString());
            }
            log.Write("ManagerStats", "--Build Dependencies--", AwaitingBuildDependenciesList.Count.ToString());
            for (int i = 0; i < AwaitingBuildDependenciesList.Count; i++)
            {
                curr = AwaitingBuildDependenciesList[i];
                log.Write("ManagerStats", curr.ObjectNumber.ToString(), curr.ToString());
            }
            StringBuilder builder = new StringBuilder();
            builder.AppendLine();
            builder.AppendLine("light: " + LightQueue.Count.ToString() +"|build: " + BuildQueue.Count.ToString() + "|rebuild: " + RebuildQueue.Count.ToString());
            builder.AppendLine("draw: " + DrawList.Count.ToString() + "|unload: " + UnloadQueue.Count.ToString());
            log.Write("ManagerStats","--Everything Else--",builder.ToString());
        }

        public void LoadChunks(Object threadContext)
        {
            int loadedThisFrame = 0;
            Chunk curr;
            Cube air = new Cube(CubeType.Air);
            Cube dirt;

            while(m_loadQueue.Count>0 && (PER_FRAME_CHUNKS_LOAD-loadedThisFrame) >0)
            {
                curr = m_loadQueue.Dequeue();
                if (!curr.LoadFromDisk())
                {
                    //Generate using terrain generation
                    for (int x = 0; x < Chunk.WIDTH; x++)
                        for (int y = 0; y < Chunk.HEIGHT; y++)
                            for (int z = 0; z < Chunk.WIDTH; z++)
                            {
                                if (y <= Chunk.HEIGHT * 0.5f + XerUtilities.Common.MathLib.NextRandom() * 20.0f)
                                {
                                    dirt = new Cube(CubeType.Stone);
                                    curr.SetCube(x, y, z, ref dirt);
                                }
                                else curr.SetCube(x, y, z, ref air);
                            }
                }

                loadedThisFrame += 1;

                curr.Loaded = true;

                AwaitingLightDependenciesList.Add(curr);
                m_awaitingLightSortNeeded = true;
                ChunkLoadedEvent(this, curr);                                
            }
            loadDone = true;
        }

        public void LightChunks(Object threadContext)
        {
            int chunksLitThisFrame = 0;

            Chunk curr;
            Chunk posX = null;
            Chunk negX = null;
            Chunk posZ = null;
            Chunk negZ = null;
            while (LightQueue.Count > 0 && PER_FRAME_CHUNKS_LIGHT - chunksLitThisFrame >= 0)
            {
                curr = LightQueue.Peek();
                negX = m_chunkStorage.GetChunk(curr.Coords.X - 1, curr.Coords.Z);
                posX = m_chunkStorage.GetChunk(curr.Coords.X + 1, curr.Coords.Z);
                negZ = m_chunkStorage.GetChunk(curr.Coords.X, curr.Coords.Z - 1);
                posZ = m_chunkStorage.GetChunk(curr.Coords.X, curr.Coords.Z + 1);

                curr.PropagateSun(posX, negX, posZ, negZ);

                chunksLitThisFrame += 1;

                AwaitingBuildDependenciesList.Add(curr);
                m_awaitingBuildSortNeeded = true;
                ChunkLitEvent(this, curr);
                LightQueue.Dequeue();
            }

            lightDone = true;
        }

        public void BuildChunkVertices(Object threadContext)
        {
            int chunksBuiltThisFrame = 0;

            Chunk curr;
            Chunk posX = null;
            Chunk negX = null;
            Chunk posZ = null;
            Chunk negZ = null;
            while (BuildQueue.Count > 0 && PER_FRAME_CHUNKS_BUILD - chunksBuiltThisFrame >= 0)
            {
                curr = BuildQueue.Dequeue();
                negX = m_chunkStorage.GetChunk(curr.Coords.X - 1, curr.Coords.Z);
                posX = m_chunkStorage.GetChunk(curr.Coords.X + 1, curr.Coords.Z);
                negZ = m_chunkStorage.GetChunk(curr.Coords.X, curr.Coords.Z - 1);
                posZ = m_chunkStorage.GetChunk(curr.Coords.X, curr.Coords.Z + 1);

                curr.BuildVertices(m_vertexBuffer, m_graphics, posX, negX, posZ, negZ);

                if(curr.Meshes.Count > 0 && !DrawList.Contains(curr)) DrawList.Add(curr);

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
                for (int i = 0; i < UnloadQueue.Count; i++)
                {

                    temp = UnloadQueue.Dequeue();
                    if(temp.ChangedSinceLoad) temp.SaveToDisk();                    
                    temp.Dispose();
                }
            }

            unloadDone = true;
        }
    }
}
