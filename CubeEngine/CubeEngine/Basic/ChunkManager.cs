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
    using System.Diagnostics;
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
        public const int PER_TICK_MAX_QUEUED = 1;
        public const int PER_TICK_CHUNKS_LOAD = 1;
        public const int PER_TICK_CHUNKS_LIGHT = 1;
        public const int PER_TICK_CHUNKS_BUILD = 1;
        public const float TIME_BETWEEN_QUEUES = .09f;
        public const float TIME_BETWEEN_LOADS = .5f;
        public const float TIME_BETWEEN_LIGHTS = .5f;
        public const float TIME_BETWEEN_BUILDS = .5f;
        public const int CHUNK_BUILD_DIST = 5;
        public const int CHUNK_LOAD_DIST = CHUNK_BUILD_DIST + 2;
        public const int INITIAL_VERTEX_ARRAY_SIZE = 1;
        public const int MAX_LIGHTING_CACHE = CHUNK_BUILD_DIST * CHUNK_BUILD_DIST * 4;


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


        //Manager Stats
        public Stopwatch watch1;
        public Stopwatch watch2;
        public float TotalUpdateTime;
        public float QueueTime;
        public float LoadTime;
        public float LightTime;
        public float BuildTime;
        public float RebuildTime;
        public float UpdateTime;
        public float UnloadTime;

        //Thread control
        public bool UseThreading;
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

        private float m_timeSinceQueue;
        private float m_timeSinceLoad;
        private float m_timeSinceLight;
        private float m_timeSinceBuild;

        //TODO: Switch to array and figure out how to be able to use it without deallocating.
        private CubeVertex[] m_vertexBuffer;

        public int LoadedChunkCount { get { return m_chunkStorage.LoadedChunkCount; } }

        public ChunkManager(GraphicsDevice graphics, ChunkCoords chunkPlayerIsIn, Vector3 positionInChunk, bool useThreading)
        {
            m_graphics = graphics;

            UseThreading = useThreading;

            AwaitingLightDependenciesList = new List<Chunk>();
            AwaitingBuildDependenciesList = new List<Chunk>();
            m_awaitingLightSortNeeded = false;
            m_awaitingBuildSortNeeded = false;

            m_chunkStorage = new ChunkStorage(CHUNK_LOAD_DIST + 1);
            m_loadQueue = new Queue<Chunk>();
            m_vertexBuffer = new CubeVertex[INITIAL_VERTEX_ARRAY_SIZE];

            LightQueue = new Queue<Chunk>();
            BuildQueue = new Queue<Chunk>();
            RebuildQueue = new Queue<Chunk>();
            UnloadQueue = new Queue<Chunk>();
            DrawList = new List<Chunk>();

            m_timeSinceQueue = TIME_BETWEEN_QUEUES;
            m_timeSinceLoad = TIME_BETWEEN_LOADS;
            m_timeSinceLight = TIME_BETWEEN_LIGHTS * 0.5f;
            m_timeSinceBuild = 0.0f;

            loadDone = true;
            buildDone = true;
            unloadDone = true;
            lightDone = true;

            //Initialize chunk loading.
            positionInChunk = -positionInChunk;
            Chunk seed = new Chunk(this, ref chunkPlayerIsIn, ref positionInChunk);
            m_loadQueue.Enqueue(seed);
            m_chunkStorage.Store(seed, this);

            watch1 = new Stopwatch();
            watch2 = new Stopwatch();

            Thread thread = new Thread(LoadChunks);
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Lowest;
            thread.Start();

            thread = new Thread(LightChunks);
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Lowest;
            thread.Start();

            thread = new Thread(BuildChunkVertices);
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.BelowNormal;
            thread.Start();
        }

        public void Update(float dt, ChunkCoords playerPosition, Vector3 deltaPosition)
        {
            watch1.Start();

            //Queue chunks for processing            
            m_timeSinceQueue += dt;
            if (m_timeSinceQueue > TIME_BETWEEN_QUEUES)
            {
                m_timeSinceQueue -= TIME_BETWEEN_QUEUES;

                watch2.Start();
                QueueChunks(playerPosition);
                watch2.Stop();
                QueueTime = (float)watch2.Elapsed.TotalMilliseconds;
            }

            //Load chunks from memory or build chunks use generation algorithms
            if (!UseThreading)
            {
                m_timeSinceLoad += dt;
                if (m_timeSinceLoad > TIME_BETWEEN_LOADS)
                {
                    m_timeSinceLoad -= TIME_BETWEEN_LOADS;

                    watch2.Reset();
                    watch2.Start();
                    LoadChunks(null);
                    watch2.Stop();
                    LoadTime = (float)watch2.Elapsed.TotalMilliseconds;
                }

                //Light chunks using "global" lighting techniques
                m_timeSinceLight += dt;
                if (m_timeSinceLight > TIME_BETWEEN_LIGHTS)
                {
                    m_timeSinceLight -= TIME_BETWEEN_LIGHTS;

                    watch2.Reset();
                    watch2.Start();
                    LightChunks(null);
                    watch2.Stop();
                    LightTime = (float)watch2.Elapsed.TotalMilliseconds;
                }

                //Build chunk vertices from the gathered data
                m_timeSinceBuild += dt;
                if (m_timeSinceBuild > TIME_BETWEEN_BUILDS)
                {
                    m_timeSinceBuild -= TIME_BETWEEN_BUILDS;

                    watch2.Reset();
                    watch2.Start();
                    BuildChunkVertices(null);
                    watch2.Stop();
                    BuildTime = (float)watch2.Elapsed.TotalMilliseconds;
                }
            }


            //Rebuild chunk vertices, always done sequentially to ensure that changes are immediately noticed by player.
            watch2.Reset();
            watch2.Start();
            RebuildChunkVertices();
            watch2.Stop();
            RebuildTime = (float)watch2.Elapsed.TotalMilliseconds;

            //Unload chunks by transferring them back to hard drive with changes and then release to object pool (eventually)
            watch2.Reset();
            watch2.Start();
            if (UnloadQueue.Count > 0 && unloadDone && UseThreading)
            {
                unloadDone = false;
                ThreadPool.UnsafeQueueUserWorkItem(UnloadChunks,false);
            }
            else if (!UseThreading)
            {
                UnloadChunks(false);
            }
            watch2.Stop();
            UnloadTime = (float)watch2.Elapsed.TotalMilliseconds;

            //Update all chunks
            watch2.Reset();
            watch2.Start();
            m_chunkStorage.UpdateChunks(dt, deltaPosition);
            watch2.Stop();
            UpdateTime = (float)watch2.Elapsed.TotalMilliseconds;

            //Store previous player position
            m_previousPlayerPosition = playerPosition;

            watch1.Stop();
            TotalUpdateTime = (float)watch1.Elapsed.TotalMilliseconds;

            watch1.Reset();
            watch2.Reset();
        }

        public void QueueChunks(ChunkCoords playerPosition)
        {

            if (m_awaitingLightSortNeeded) AwaitingLightDependenciesList.Sort((x, y) => x.Coords.CompareDistance(ref y.Coords, ref playerPosition));
            if (m_awaitingBuildSortNeeded) AwaitingBuildDependenciesList.Sort((x, y) => x.Coords.CompareDistance(ref y.Coords, ref playerPosition));

            Chunk curr;
            Chunk other;
            ChunkCoords coords;
            Vector3 newPos;
            int numAddedToLoadQueue = 0;
            for (int i = 0; i < AwaitingLightDependenciesList.Count; i++)
            {
                curr = AwaitingLightDependenciesList[i];
                if (curr.Unloading)
                {
                    AwaitingLightDependenciesList.Remove(curr);
                    continue;
                }
                if (curr.Coords.Distance(ref playerPosition) <= CHUNK_LOAD_DIST)
                {
                    if (curr.LoadDependenciesFlag != Chunk.DEPENDENCIES_MET_FLAG_VALUE && numAddedToLoadQueue < PER_TICK_MAX_QUEUED)// && !(AwaitingLightDependenciesList.Count > MAX_LIGHTING_CACHE))
                    {
                        //Use the chunk awaiting lighting as a seed for loading the blocks around it if
                        //they aren't already loaded.
                        //+x
                        if (((curr.LoadDependenciesFlag & 1) != 1))
                        {
                            curr.Coords.GetShiftedX(1, out coords);
                            if (!m_chunkStorage.Contains(ref coords))
                            {
                                newPos = Vector3.Multiply(Vector3.Right, Chunk.WIDTH);
                                Vector3.Add(ref curr.Position, ref newPos, out newPos);
                                other = new Chunk(this, ref coords, ref newPos);
                                m_chunkStorage.Store(other, this);
                                m_loadQueue.Enqueue(other);
                                ChunkLoadingEvent(this, other);
                                numAddedToLoadQueue++;
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
                                newPos = Vector3.Multiply(Vector3.Left, Chunk.WIDTH);
                                Vector3.Add(ref curr.Position, ref newPos, out newPos);
                                other = new Chunk(this, ref coords, ref newPos);
                                m_chunkStorage.Store(other, this);
                                m_loadQueue.Enqueue(other);
                                ChunkLoadingEvent(this, other);
                                numAddedToLoadQueue++;
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
                                newPos = Vector3.Multiply(Vector3.Backward, Chunk.WIDTH);
                                Vector3.Add(ref curr.Position, ref newPos, out newPos);
                                other = new Chunk(this, ref coords, ref newPos);
                                m_chunkStorage.Store(other, this);
                                m_loadQueue.Enqueue(other);
                                ChunkLoadingEvent(this, other);
                                numAddedToLoadQueue++;
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
                                newPos = Vector3.Multiply(Vector3.Forward, Chunk.WIDTH);
                                Vector3.Add(ref curr.Position, ref newPos, out newPos);
                                other = new Chunk(this, ref coords, ref newPos);
                                m_chunkStorage.Store(other, this);
                                m_loadQueue.Enqueue(other);
                                ChunkLoadingEvent(this, other);
                                numAddedToLoadQueue++;
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
                    else if (curr.LoadDependenciesFlag == 15)
                    {
                        //+x
                        if (((curr.LightDependenciesFlag & 1) != 1))
                        {
                            curr.Coords.GetShiftedX(1, out coords);
                            other = m_chunkStorage.GetChunk(coords.X, coords.Z);
                            if (other.Loaded == true && curr.Coords.Neighbors(ref other.Coords) == 1)
                            {
                                curr.LightDependenciesFlag |= 1;
                            }
                        }
                        //-x
                        if (((curr.LightDependenciesFlag & 2) != 2))
                        {
                            curr.Coords.GetShiftedX(-1, out coords);
                            other = m_chunkStorage.GetChunk(coords.X, coords.Z);
                            if (other.Loaded == true && curr.Coords.Neighbors(ref other.Coords) == 2)
                            {
                                curr.LightDependenciesFlag |= 2;
                            }
                        }
                        //+z
                        if (((curr.LightDependenciesFlag & 4) != 4))
                        {
                            curr.Coords.GetShiftedZ(1, out coords);
                            other = m_chunkStorage.GetChunk(coords.X, coords.Z);
                            if (other.Loaded == true && curr.Coords.Neighbors(ref other.Coords) == 4)
                            {
                                curr.LightDependenciesFlag |= 4;
                            }
                        }
                        //-z
                        if (((curr.LightDependenciesFlag & 8) != 8))
                        {
                            curr.Coords.GetShiftedZ(-1, out coords);
                            other = m_chunkStorage.GetChunk(coords.X, coords.Z);
                            if (other.Loaded == true && curr.Coords.Neighbors(ref other.Coords) == 8)
                            {
                                curr.LightDependenciesFlag |= 8;
                            }
                        }

                    }
                }
            }

            for (int i = 0; i < AwaitingBuildDependenciesList.Count; i++)
            {
                curr = AwaitingBuildDependenciesList[i];
                if (curr.Unloading)
                {
                    AwaitingBuildDependenciesList.Remove(curr);
                    continue;
                }
                if (curr.Coords.Distance(ref playerPosition) <= CHUNK_BUILD_DIST)
                {
                    if (curr.BuildDependenciesFlag == Chunk.DEPENDENCIES_MET_FLAG_VALUE)
                    {
                        BuildQueue.Enqueue(curr);
                        AwaitingBuildDependenciesList.Remove(curr);
                    }
                }
            }
        }

        public void LoadChunks(Object threadContext)
        {
            
            Chunk curr;
            Cube air = new Cube(CubeType.Air);
            Cube dirt;

            if (UseThreading)
            {
                while (loadDone)
                {
                    Monitor.Enter(m_loadQueue);
                    Monitor.Wait(m_loadQueue, 100); while (m_loadQueue.Count == 0) Monitor.Wait(m_loadQueue, 100);
                    Monitor.Exit(m_loadQueue);

                    while (m_loadQueue.Count > 0)
                    {
                        curr = m_loadQueue.Dequeue();
                        if (curr.Unloading) continue;
                        
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
                        curr.Loaded = true;

                        AwaitingLightDependenciesList.Add(curr);
                        m_awaitingLightSortNeeded = true;
                        ChunkLoadedEvent(this, curr);
                    }
                }
            }
            else            
            {
                int loadedThisFrame = 0;
                while (m_loadQueue.Count > 0 && (PER_TICK_CHUNKS_LOAD - loadedThisFrame) > 0)
                {
                    curr = m_loadQueue.Dequeue();
                    if (curr.Unloading) continue;

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
            }
        }

        public void LightChunks(Object threadContext)
        {
            

            Chunk curr;
            Chunk posX = null;
            Chunk negX = null;
            Chunk posZ = null;
            Chunk negZ = null;

            if (UseThreading)
            {
                while (lightDone)
                {
                    Monitor.Enter(LightQueue);
                    Monitor.Wait(LightQueue, 100);//while (LightQueue.Count == 0) Monitor.Wait(LightQueue, 500);
                    Monitor.Exit(LightQueue);

                    while (LightQueue.Count > 0)
                    {
                        curr = LightQueue.Dequeue();
                        if (curr.Unloading) continue;

                        negX = m_chunkStorage.GetChunk(curr.Coords.X - 1, curr.Coords.Z);
                        posX = m_chunkStorage.GetChunk(curr.Coords.X + 1, curr.Coords.Z);
                        negZ = m_chunkStorage.GetChunk(curr.Coords.X, curr.Coords.Z - 1);
                        posZ = m_chunkStorage.GetChunk(curr.Coords.X, curr.Coords.Z + 1);

                        curr.PropagateSun(posX, negX, posZ, negZ);

                        AwaitingBuildDependenciesList.Add(curr);
                        m_awaitingBuildSortNeeded = true;
                        ChunkLitEvent(this, curr);
                    }
                }
            }
            else
            {
                int chunksLitThisFrame = 0;
                while (LightQueue.Count > 0 && PER_TICK_CHUNKS_LIGHT - chunksLitThisFrame >= 0)
                {
                    curr = LightQueue.Dequeue();
                    if (curr.Unloading) continue;

                    negX = m_chunkStorage.GetChunk(curr.Coords.X - 1, curr.Coords.Z);
                    posX = m_chunkStorage.GetChunk(curr.Coords.X + 1, curr.Coords.Z);
                    negZ = m_chunkStorage.GetChunk(curr.Coords.X, curr.Coords.Z - 1);
                    posZ = m_chunkStorage.GetChunk(curr.Coords.X, curr.Coords.Z + 1);

                    curr.PropagateSun(posX, negX, posZ, negZ);

                    chunksLitThisFrame += 1;

                    AwaitingBuildDependenciesList.Add(curr);
                    m_awaitingBuildSortNeeded = true;
                    ChunkLitEvent(this, curr);
                }
            }
        }

        public void BuildChunkVertices(Object threadContext)
        { 
            Chunk curr;
            Chunk posX = null;
            Chunk negX = null;
            Chunk posZ = null;
            Chunk negZ = null;

            if (UseThreading)
            {
                while (buildDone)
                {
                    Monitor.Enter(BuildQueue);
                    Monitor.Wait(BuildQueue, 100); //while (BuildQueue.Count == 0) Monitor.Wait(BuildQueue, 500);
                    Monitor.Exit(BuildQueue);

                    while (BuildQueue.Count > 0)
                    {
                        curr = BuildQueue.Dequeue();
                        if (curr.Unloading) continue;

                        negX = m_chunkStorage.GetChunk(curr.Coords.X - 1, curr.Coords.Z);
                        posX = m_chunkStorage.GetChunk(curr.Coords.X + 1, curr.Coords.Z);
                        negZ = m_chunkStorage.GetChunk(curr.Coords.X, curr.Coords.Z - 1);
                        posZ = m_chunkStorage.GetChunk(curr.Coords.X, curr.Coords.Z + 1);

                        curr.BuildVertices(m_vertexBuffer, m_graphics, posX, negX, posZ, negZ);

                        if (curr.Meshes.Count > 0 && !DrawList.Contains(curr)) DrawList.Add(curr);
                    }
                }
            }
            else
            {
                int chunksBuiltThisFrame = 0;
                while (BuildQueue.Count > 0 && PER_TICK_CHUNKS_BUILD - chunksBuiltThisFrame >= 0)
                {
                    curr = BuildQueue.Dequeue();
                    if (curr.Unloading) continue;

                    negX = m_chunkStorage.GetChunk(curr.Coords.X - 1, curr.Coords.Z);
                    posX = m_chunkStorage.GetChunk(curr.Coords.X + 1, curr.Coords.Z);
                    negZ = m_chunkStorage.GetChunk(curr.Coords.X, curr.Coords.Z - 1);
                    posZ = m_chunkStorage.GetChunk(curr.Coords.X, curr.Coords.Z + 1);

                    curr.BuildVertices(m_vertexBuffer, m_graphics, posX, negX, posZ, negZ);

                    if (curr.Meshes.Count > 0 && !DrawList.Contains(curr)) DrawList.Add(curr);

                    chunksBuiltThisFrame += 1;
                }
            }
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

        public void PrepareChunkForUnload(Chunk chunk)
        {
            chunk.Unloading = true;
            ChunkLoadingEvent -= chunk.ChunkLoadingCallback;
            ChunkLoadedEvent -= chunk.ChunkLoadedCallback;
            ChunkLitEvent -= chunk.ChunkLitCallback;
            DrawList.Remove(chunk);
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
            builder.AppendLine("light: " + LightQueue.Count.ToString() + "|build: " + BuildQueue.Count.ToString() + "|rebuild: " + RebuildQueue.Count.ToString());
            builder.AppendLine("draw: " + DrawList.Count.ToString() + "|unload: " + UnloadQueue.Count.ToString());
            log.Write("ManagerStats", "--Everything Else--", builder.ToString());
        }
    }
}
