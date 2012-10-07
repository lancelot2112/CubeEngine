using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

using CubeEngine.Rendering;
using CubeEngine.Utilities.Noise;

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
        public const int PER_TICK_MAX_QUEUED = 2;
        public const int PER_TICK_CHUNKS_LOAD = 1;
        public const int PER_TICK_CHUNKS_LIGHT = 1;
        public const int PER_TICK_CHUNKS_BUILD = 1;
        public const float TIME_BETWEEN_QUEUES = .05f;
        public const float TIME_BETWEEN_LOADS = .5f;
        public const float TIME_BETWEEN_LIGHTS = .5f;
        public const float TIME_BETWEEN_BUILDS = .5f;
        public const int CHUNK_BUILD_DIST = 4;
        public const int CHUNK_LIGHT_DIST = CHUNK_BUILD_DIST + 2;
        public const int CHUNK_LOAD_DIST = CHUNK_LIGHT_DIST + 2;
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
        private bool _awaitingLightSortNeeded;
        public List<Chunk> AwaitingBuildDependenciesList;
        private bool _awaitingBuildSortNeeded;


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

        public CubeStorage CubeStorage;
        private ChunkStorage _chunkStorage;
        private Queue<Chunk> _loadQueue;
        public Queue<Chunk> LightQueue;
        public Queue<Chunk> BuildQueue;
        public Queue<Chunk> RebuildQueue;
        public Queue<Chunk> UnloadQueue;
        public List<Chunk> DrawList;

        private ChunkCoords m_previousPlayerPosition;

        private float _timeSinceQueue;
        private float _timeSinceLoad;
        private float _timeSinceLight;
        private float _timeSinceBuild;

        //TODO: Switch to array and figure out how to be able to use it without deallocating.
        private CubeVertex[] _vertexBuffer;

        public int LoadedChunkCount { get { return _chunkStorage.LoadedChunkCount; } }

        public ChunkManager(GraphicsDevice graphics, ChunkCoords chunkPlayerIsIn, Vector3 positionInChunk, bool useThreading)
        {
            m_graphics = graphics;

            UseThreading = useThreading;

            AwaitingLightDependenciesList = new List<Chunk>();
            AwaitingBuildDependenciesList = new List<Chunk>();
            _awaitingLightSortNeeded = false;
            _awaitingBuildSortNeeded = false;

            int width = 1;
            while (width < 2*CHUNK_LOAD_DIST) width *= 2;
            width *= Chunk.WIDTH;
            CubeStorage = new CubeStorage(width, Chunk.HEIGHT, width);
            _chunkStorage = new ChunkStorage(CHUNK_LOAD_DIST);
            _loadQueue = new Queue<Chunk>();
            _vertexBuffer = new CubeVertex[1000];

            LightQueue = new Queue<Chunk>();
            BuildQueue = new Queue<Chunk>();
            RebuildQueue = new Queue<Chunk>();
            UnloadQueue = new Queue<Chunk>();
            DrawList = new List<Chunk>();

            _timeSinceQueue = TIME_BETWEEN_QUEUES;
            _timeSinceLoad = TIME_BETWEEN_LOADS;
            _timeSinceLight = TIME_BETWEEN_LIGHTS * 0.5f;
            _timeSinceBuild = 0.0f;

            loadDone = true;
            buildDone = true;
            unloadDone = true;
            lightDone = true;

            //Initialize chunk loading.
            positionInChunk = -positionInChunk;
            Chunk seed = new Chunk(this, ref chunkPlayerIsIn, ref positionInChunk);
            _loadQueue.Enqueue(seed);
            _chunkStorage.Store(seed, this);

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
            _timeSinceQueue += dt;
            if (_timeSinceQueue > TIME_BETWEEN_QUEUES)
            {
                _timeSinceQueue -= TIME_BETWEEN_QUEUES;

                watch2.Start();
                QueueChunks(playerPosition);
                watch2.Stop();
                QueueTime = (float)watch2.Elapsed.TotalMilliseconds;
            }

            //Rebuild chunk vertices, always done sequentially to ensure that changes are immediately noticed by player.
            watch2.Reset();
            watch2.Start();
            RebuildChunkVertices();
            watch2.Stop();
            RebuildTime = (float)watch2.Elapsed.TotalMilliseconds;

            //Unload chunks by transferring them back to hard drive with changes and then release to object pool (eventually)
            if (UnloadQueue.Count > 0 && unloadDone)
            {
                unloadDone = false;
                ThreadPool.UnsafeQueueUserWorkItem(UnloadChunks,false);
            }

            //Update all chunks
            watch2.Reset();
            watch2.Start();
            _chunkStorage.UpdateChunks(dt, deltaPosition);
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

            if (_awaitingLightSortNeeded) AwaitingLightDependenciesList.Sort((x, y) => x.Coords.CompareDistance(ref y.Coords, ref playerPosition));
            if (_awaitingBuildSortNeeded) AwaitingBuildDependenciesList.Sort((x, y) => x.Coords.CompareDistance(ref y.Coords, ref playerPosition));

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
                            if (!_chunkStorage.Contains(ref coords))
                            {
                                newPos = Vector3.Multiply(Vector3.Right, Chunk.WIDTH);
                                Vector3.Add(ref curr.Position, ref newPos, out newPos);
                                other = new Chunk(this, ref coords, ref newPos);
                                _chunkStorage.Store(other, this);
                                _loadQueue.Enqueue(other);
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
                            if (!_chunkStorage.Contains(ref coords))
                            {
                                newPos = Vector3.Multiply(Vector3.Left, Chunk.WIDTH);
                                Vector3.Add(ref curr.Position, ref newPos, out newPos);
                                other = new Chunk(this, ref coords, ref newPos);
                                _chunkStorage.Store(other, this);
                                _loadQueue.Enqueue(other);
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
                            if (!_chunkStorage.Contains(ref coords))
                            {
                                newPos = Vector3.Multiply(Vector3.Backward, Chunk.WIDTH);
                                Vector3.Add(ref curr.Position, ref newPos, out newPos);
                                other = new Chunk(this, ref coords, ref newPos);
                                _chunkStorage.Store(other, this);
                                _loadQueue.Enqueue(other);
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
                            if (!_chunkStorage.Contains(ref coords))
                            {
                                newPos = Vector3.Multiply(Vector3.Forward, Chunk.WIDTH);
                                Vector3.Add(ref curr.Position, ref newPos, out newPos);
                                other = new Chunk(this, ref coords, ref newPos);
                                _chunkStorage.Store(other, this);
                                _loadQueue.Enqueue(other);
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
                            other = _chunkStorage.GetChunk(coords.X, coords.Z);
                            if (other.Loaded == true && curr.Coords.Neighbors(ref other.Coords) == 1)
                            {
                                curr.LightDependenciesFlag |= 1;
                            }
                        }
                        //-x
                        if (((curr.LightDependenciesFlag & 2) != 2))
                        {
                            curr.Coords.GetShiftedX(-1, out coords);
                            other = _chunkStorage.GetChunk(coords.X, coords.Z);
                            if (other.Loaded == true && curr.Coords.Neighbors(ref other.Coords) == 2)
                            {
                                curr.LightDependenciesFlag |= 2;
                            }
                        }
                        //+z
                        if (((curr.LightDependenciesFlag & 4) != 4))
                        {
                            curr.Coords.GetShiftedZ(1, out coords);
                            other = _chunkStorage.GetChunk(coords.X, coords.Z);
                            if (other.Loaded == true && curr.Coords.Neighbors(ref other.Coords) == 4)
                            {
                                curr.LightDependenciesFlag |= 4;
                            }
                        }
                        //-z
                        if (((curr.LightDependenciesFlag & 8) != 8))
                        {
                            curr.Coords.GetShiftedZ(-1, out coords);
                            other = _chunkStorage.GetChunk(coords.X, coords.Z);
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

        ChunkNoise noise = new ChunkNoise(1);
        public void LoadChunks(Object threadContext)
        {
            
            Chunk curr;
            Cube air = new Cube(CubeType.Air);
            Cube cube;
            float noisevalue;
            int worldX;
            int worldZ;
            int height;
            float invHeight = 1.0f/Chunk.HEIGHT;

            float[,] heightmap = new float[Chunk.WIDTH, Chunk.WIDTH];

            while (loadDone)
            {
                Monitor.Enter(_loadQueue);
                Monitor.Wait(_loadQueue, 100); while (_loadQueue.Count == 0) Monitor.Wait(_loadQueue, 100);
                Monitor.Exit(_loadQueue);

                while (_loadQueue.Count > 0)
                {
                    curr = _loadQueue.Dequeue();
                    if (curr.Unloading) continue;

                    if (!curr.LoadFromDisk())
                    {
                        //Generate using terrain generation
                        //Get the heightmap
                        noise.FillMap2D(heightmap, curr.Coords.X, curr.Coords.Z, octaves: 5, startFrequency: .03f, startAmplitude: 5);
                        for (int x = 0; x < Chunk.WIDTH; x++)
                        {
                            worldX = x + curr.Coords.X * Chunk.WIDTH;
                            for (int z = 0; z < Chunk.WIDTH; z++)
                            {
                                worldZ = z + curr.Coords.Z * Chunk.WIDTH;

                                height = (int)(heightmap[x, z] + Settings.SEA_LEVEL);

                                //Create ground
                                for (int y = 0; y < height; y++)
                                {
                                    cube = new Cube(CubeType.Stone);
                                    CubeStorage.SetCube(worldX, y, worldZ, ref cube);
                                }

                                //Create mountains
                                for (int y = height; y < Chunk.HEIGHT; y++)
                                {
                                    noisevalue = noise.GetValue3D(worldX, y, worldZ, octaves: 6, startFrequency: .05f, startAmplitude: 2);
                                    MathHelper.Clamp(noisevalue, -1, 1);
                                    noisevalue -= 2 * height * invHeight;

                                    if (noisevalue > 0.5f)
                                    {
                                        cube = new Cube(CubeType.Stone);
                                        CubeStorage.SetCube(worldX, y, worldZ, ref cube);
                                    }
                                    else if (noisevalue > 0.0f)
                                    {
                                        cube = new Cube(CubeType.Dirt);
                                        CubeStorage.SetCube(worldX, y, worldZ, ref cube);
                                    }
                                    else
                                    {
                                        cube = new Cube(CubeType.Air);
                                        CubeStorage.SetCube(worldX, y, worldZ, ref cube);
                                    }
                                }

                            }
                        }
                    }
                    curr.Loaded = true;

                    AwaitingLightDependenciesList.Add(curr);
                    _awaitingLightSortNeeded = true;
                    ChunkLoadedEvent(this, curr);
                }
            }
            

            
        }

        public void LightChunks(Object threadContext)
        {
            

            Chunk curr;

            while (lightDone)
            {
                Monitor.Enter(LightQueue);
                Monitor.Wait(LightQueue, 100);//while (LightQueue.Count == 0) Monitor.Wait(LightQueue, 500);
                Monitor.Exit(LightQueue);

                while (LightQueue.Count > 0)
                {
                    curr = LightQueue.Dequeue();
                    if (curr.Unloading) continue;

                    curr.PropagateSun(CubeStorage);

                    AwaitingBuildDependenciesList.Add(curr);
                    _awaitingBuildSortNeeded = true;
                    ChunkLitEvent(this, curr);
                }
            }
        }

        public void BuildChunkVertices(Object threadContext)
        { 
            Chunk curr;
            while (buildDone)
            {
                Monitor.Enter(BuildQueue);
                Monitor.Wait(BuildQueue, 100); //while (BuildQueue.Count == 0) Monitor.Wait(BuildQueue, 500);
                Monitor.Exit(BuildQueue);

                while (BuildQueue.Count > 0)
                {
                    curr = BuildQueue.Dequeue();
                    if (curr.Unloading) continue;

                    curr.BuildVertices(_vertexBuffer, m_graphics, CubeStorage);

                    if (curr.Meshes.Count > 0 && !DrawList.Contains(curr)) DrawList.Add(curr);
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
