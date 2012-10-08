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
        private GraphicsDevice _graphics;

        /// <summary>
        /// Configurable settings to set how far to load blocks.
        /// </summary>
        public const int PER_TICK_MAX_QUEUED = 2;
        public const int PER_TICK_CHUNKS_LOAD = 5;
        public const int PER_TICK_CHUNKS_LIGHT = 3;
        public const int PER_TICK_CHUNKS_BUILD = 2;
        public const float TIME_BETWEEN_QUEUES = .05f;
        public const float TIME_BETWEEN_LOADS = .5f;
        public const float TIME_BETWEEN_LIGHTS = .5f;
        public const float TIME_BETWEEN_BUILDS = .5f;
        public static int CHUNK_BUILD_DIST = 4;
        public static int CHUNK_LOAD_DIST = 8;
        public static int CHUNK_LIGHT_DIST = CHUNK_LOAD_DIST - 2;
        

        //public static int xOffset = (int)(xChunkNumber * 0.5f);
        //public static int yOffset = (int)(yChunkNumber * 0.5f);
        //public static int zOffset = (int)(zChunkNumber * 0.5f);

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
        private volatile bool _loadThreadContinue;
        private volatile bool _buildThreadContinue;
        private volatile bool _unloadThreadContinue;
        private volatile bool _lightThreadContinue;
        private volatile int _numToLoad;
        private volatile int _numToLight;
        private volatile int _numToBuild;
        //private ManualResetEvent cullDone;

        public readonly CubeStorage CubeStorage;
        public readonly ChunkStorage ChunkStorage;
        private List<Chunk> _loadQueue;
        public List<Chunk> LightQueue;
        public List<Chunk> BuildQueue;
        public Queue<Chunk> RebuildQueue;
        public Queue<Chunk> UnloadQueue;
        public List<Chunk> DrawList;

        public ChunkCoords PlayerPosition;
        public ChunkCoords PreviousPlayerPosition;

        private float _timeSinceQueue;
        private float _timeSinceLoad;
        private float _timeSinceLight;
        private float _timeSinceBuild;

        private int _chunksQueuedThisFrame;

        //TODO: Switch to array and figure out how to be able to use it without deallocating.
        private CubeVertex[] _vertexBuffer;

        public int LoadedChunkCount { get { return ChunkStorage.LoadedChunkCount; } }

        public ChunkManager(GraphicsDevice graphics, ChunkCoords chunkPlayerIsIn, Vector3 positionInChunk, bool useThreading)
        {
            _graphics = graphics;

            UseThreading = useThreading;

            int width = 1;
            while (width < 2*CHUNK_LOAD_DIST) width *= 2;
            ChunkStorage = new ChunkStorage(width);
            CHUNK_LOAD_DIST = width / 2;
            CHUNK_LIGHT_DIST = CHUNK_LOAD_DIST - 2;
            CHUNK_BUILD_DIST = CHUNK_LIGHT_DIST - 1;
            width *= Chunk.WIDTH;
            CubeStorage = new CubeStorage(width, Chunk.HEIGHT, width);
            
            _loadQueue = new List<Chunk>();
            _vertexBuffer = new CubeVertex[1000];

            LightQueue = new List<Chunk>();
            BuildQueue = new List<Chunk>();
            RebuildQueue = new Queue<Chunk>();
            UnloadQueue = new Queue<Chunk>();
            DrawList = new List<Chunk>();

            _timeSinceQueue = TIME_BETWEEN_QUEUES;
            _timeSinceLoad = TIME_BETWEEN_LOADS;
            _timeSinceLight = TIME_BETWEEN_LIGHTS * 0.5f;
            _timeSinceBuild = 0.0f;

            _loadThreadContinue = true;
            _buildThreadContinue = true;
            _unloadThreadContinue = true;
            _lightThreadContinue = true;

            //Initialize chunk loading.
            positionInChunk = -positionInChunk;
            Chunk seed = new Chunk(this, ref chunkPlayerIsIn, ref positionInChunk);
            _loadQueue.Add(seed);
            ChunkStorage.Store(seed, this);

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

        public void AddChunk(Chunk chunk)
        {
            ChunkStorage.Store(chunk, this);
            _loadQueue.Add(chunk);            
            _chunksQueuedThisFrame++;
        }

        public void Update(float dt, ChunkCoords playerPosition, Vector3 deltaPosition)
        {
            watch1.Start();

            _numToLoad = PER_TICK_CHUNKS_LOAD;
            _numToLight = PER_TICK_CHUNKS_LIGHT;
            _numToBuild = PER_TICK_CHUNKS_BUILD;

            PlayerPosition = playerPosition;

            //Queue chunks for processing            
            _timeSinceQueue += dt;
            if (_timeSinceQueue > TIME_BETWEEN_QUEUES)
            {
                _timeSinceQueue -= TIME_BETWEEN_QUEUES;

                watch2.Start();
                ManageChunks();
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
            if (UnloadQueue.Count > 0 && _unloadThreadContinue)
            {
                _unloadThreadContinue = false;
                ThreadPool.UnsafeQueueUserWorkItem(UnloadChunks,false);
            }

            //Update all chunks
            watch2.Reset();
            watch2.Start();
            ChunkStorage.UpdateChunks(dt, deltaPosition);
            watch2.Stop();
            UpdateTime = (float)watch2.Elapsed.TotalMilliseconds;

            //Store previous player position
            PreviousPlayerPosition = PlayerPosition;

            watch1.Stop();
            TotalUpdateTime = (float)watch1.Elapsed.TotalMilliseconds;

            watch1.Reset();
            watch2.Reset();
        }

        public void ManageChunks()
        {
            _chunksQueuedThisFrame = 0;

            if (!PlayerPosition.Equals(ref PreviousPlayerPosition) || ChunkStorage.ChunksAddedSinceSort) ChunkStorage.LoadedChunks.Sort((x, y) => x.Coords.CompareDistance(ref y.Coords, ref PlayerPosition));

            Chunk curr;
            int dist;
            for (int i = 0; i < ChunkStorage.LoadedChunks.Count; i++)
            {
                curr = ChunkStorage.LoadedChunks[i];
                if (curr.State == ChunkState.Unloading) continue;

                dist = curr.Coords.Distance(ref PlayerPosition);
                switch (curr.State)
                {
                    case ChunkState.Loaded:
                        {
                            if (dist < CHUNK_LOAD_DIST && curr.NeighborsInMemoryFlag != Chunk.DEPENDENCIES_MET_FLAG_VALUE)
                            {                                
                                QueueNeighbors(curr);
                                log.Write("queueingMan", curr.ToString(), "");
                                curr.ChangeState(this, ChunkState.PendingLight);
                            }
                            break;
                        }
                    case ChunkState.PendingLight:
                        {
                            if (dist <= CHUNK_LIGHT_DIST & curr.LightDependenciesFlag == Chunk.DEPENDENCIES_MET_FLAG_VALUE)
                            {                                
                                curr.ChangeState(this, ChunkState.Lighting);
                                log.Write("lightingMan", curr.ToString(), "");
                                LightQueue.Add(curr);                                
                            }
                            else if (dist < CHUNK_LOAD_DIST && curr.NeighborsInMemoryFlag != Chunk.DEPENDENCIES_MET_FLAG_VALUE)
                            {
                                QueueNeighbors(curr);
                            }
                            break;
                        }
                    case ChunkState.PendingBuild:
                        {
                            if (dist <= CHUNK_BUILD_DIST & curr.BuildDependenciesFlag == Chunk.DEPENDENCIES_MET_FLAG_VALUE)
                            {                                
                                curr.ChangeState(this, ChunkState.Building);
                                log.Write("buildingMan", curr.ToString(), "");
                                BuildQueue.Add(curr);
                            }
                            else if (dist < CHUNK_LOAD_DIST && curr.NeighborsInMemoryFlag != Chunk.DEPENDENCIES_MET_FLAG_VALUE)
                            {
                                QueueNeighbors(curr);
                            }
                            break;
                        }
                    case ChunkState.Built:
                        {
                            if (dist < CHUNK_LOAD_DIST && curr.NeighborsInMemoryFlag != Chunk.DEPENDENCIES_MET_FLAG_VALUE)
                            {
                                QueueNeighbors(curr);
                            }
                            break;
                        }
                }
            }
        }

        public void QueueNeighbors(Chunk chunk)
        {

            Chunk neighborchunk;
            ChunkCoords neighborcoords;
            Vector3 newpos;
            bool contains;

            //+x
            //Get new chunkcoords
            chunk.Coords.Add(1, 0, out neighborcoords);
            contains = ChunkStorage.Contains(ref neighborcoords, out neighborchunk);
            if ((chunk.NeighborsInMemoryFlag & 1) != 1)
            {
                if (!contains)
                {
                    //Shift this chunks position to get the new chunks relative position
                    newpos.X = chunk.Position.X + Chunk.WIDTH;
                    newpos.Y = chunk.Position.Y;
                    newpos.Z = chunk.Position.Z;
                    //Create a new chunk
                    neighborchunk = new Chunk(this, ref neighborcoords, ref newpos);
                    //Update flag and connect event chain to neighbor
                    chunk.NeighborsInMemoryFlag |= 1;
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
                    //Add new chunk to manager
                    AddChunk(neighborchunk);
                }
                else if (contains)//Chunk is already in memory connect to it
                {
                    //Update flag and connect event chain to neighbor
                    chunk.ConsolidateFlags(neighborchunk, 1);
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
                }
            }
            //-x
            //Get new chunkcoords
            chunk.Coords.Add(-1, 0, out neighborcoords);
            contains = ChunkStorage.Contains(ref neighborcoords, out neighborchunk);
            if ((chunk.NeighborsInMemoryFlag & 2) != 2)
            {
                if (!contains)
                {
                    //Shift this chunks position to get the new chunks relative position
                    newpos.X = chunk.Position.X - Chunk.WIDTH;
                    newpos.Y = chunk.Position.Y;
                    newpos.Z = chunk.Position.Z;
                    //Create a new chunk
                    neighborchunk = new Chunk(this, ref neighborcoords, ref newpos);
                    //Update flag and connect event chain to neighbor
                    chunk.NeighborsInMemoryFlag |= 2;
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
                    //Add new chunk to manager
                    AddChunk(neighborchunk);
                }
                else if (contains)//Chunk is already in memory connect to it
                {
                    //Update flag and connect event chain to neighbor
                    chunk.ConsolidateFlags(neighborchunk, 2);
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
                }
            }
            //+z
            //Get new chunkcoords
            chunk.Coords.Add(0, 1, out neighborcoords);
            contains = ChunkStorage.Contains(ref neighborcoords, out neighborchunk);
            if ((chunk.NeighborsInMemoryFlag & 4) != 4)
            {
                if (!contains)
                {
                    //Shift this chunks position to get the new chunks relative position
                    newpos.X = chunk.Position.X;
                    newpos.Y = chunk.Position.Y;
                    newpos.Z = chunk.Position.Z + Chunk.WIDTH;
                    //Create a new chunk
                    neighborchunk = new Chunk(this, ref neighborcoords, ref newpos);
                    //Update flag and connect event chain to neighbor
                    chunk.NeighborsInMemoryFlag |= 4;
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
                    //Add new chunk to manager
                    AddChunk(neighborchunk);
                }
                else if (contains)//Chunk is already in memory connect to it
                {
                    //Update flag and connect event chain to neighbor
                    chunk.ConsolidateFlags(neighborchunk, 4);
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
                }
            }
            //-z
            //Get new chunkcoords
            chunk.Coords.Add(0, -1, out neighborcoords);
            contains = ChunkStorage.Contains(ref neighborcoords, out neighborchunk);
            if ((chunk.NeighborsInMemoryFlag & 8) != 8)
            {
                if (!contains)
                {
                    //Shift this chunks position to get the new chunks relative position
                    newpos.X = chunk.Position.X;
                    newpos.Y = chunk.Position.Y;
                    newpos.Z = chunk.Position.Z - Chunk.WIDTH;
                    //Create a new chunk
                    neighborchunk = new Chunk(this, ref neighborcoords, ref newpos);
                    //Update flag and connect event chain to neighbor
                    chunk.NeighborsInMemoryFlag |= 8;
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
                    //Add new chunk to manager
                    AddChunk(neighborchunk);
                }
                else if (contains)//Chunk is already in memory connect to it
                {
                    //Update flag and connect event chain to neighbor
                    chunk.ConsolidateFlags(neighborchunk, 8);
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
                }
            }
            //+x +z
            //Get new chunkcoords
            chunk.Coords.Add(1, 1, out neighborcoords);
            contains = ChunkStorage.Contains(ref neighborcoords, out neighborchunk);
            if ((chunk.NeighborsInMemoryFlag & 16) != 16)
            {
                if (!contains)
                {
                    //Shift this chunks position to get the new chunks relative position
                    newpos.X = chunk.Position.X + Chunk.WIDTH;
                    newpos.Y = chunk.Position.Y;
                    newpos.Z = chunk.Position.Z + Chunk.WIDTH;
                    //Create a new chunk
                    neighborchunk = new Chunk(this, ref neighborcoords, ref newpos);
                    //Update flag and connect event chain to neighbor
                    chunk.NeighborsInMemoryFlag |= 16;
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
                    //Add new chunk to manager
                    AddChunk(neighborchunk);
                }
                else if (contains)//Chunk is already in memory connect to it
                {
                    //Update flag and connect event chain to neighbor
                    chunk.ConsolidateFlags(neighborchunk, 16);
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
                }
            }
            //-x +z
            //Get new chunkcoords
            chunk.Coords.Add(-1, 1, out neighborcoords);
            contains = ChunkStorage.Contains(ref neighborcoords, out neighborchunk);
            if ((chunk.NeighborsInMemoryFlag & 32) != 32)
            {
                if (!contains)
                {
                    //Shift this chunks position to get the new chunks relative position
                    newpos.X = chunk.Position.X - Chunk.WIDTH;
                    newpos.Y = chunk.Position.Y;
                    newpos.Z = chunk.Position.Z + Chunk.WIDTH;
                    //Create a new chunk
                    neighborchunk = new Chunk(this, ref neighborcoords, ref newpos);
                    //Update flag and connect event chain to neighbor
                    chunk.NeighborsInMemoryFlag |= 32;
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
                    //Add new chunk to manager
                    AddChunk(neighborchunk);
                }
                else if (contains)//Chunk is already in memory connect to it
                {
                    //Update flag and connect event chain to neighbor
                    chunk.ConsolidateFlags(neighborchunk, 32);
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
                }
            }
            //+x -z
            //Get new chunkcoords
            chunk.Coords.Add(1, -1, out neighborcoords);
            contains = ChunkStorage.Contains(ref neighborcoords, out neighborchunk);
            if ((chunk.NeighborsInMemoryFlag & 64) != 64)
            {
                if (!contains)
                {
                    //Shift this chunks position to get the new chunks relative position
                    newpos.X = chunk.Position.X + Chunk.WIDTH;
                    newpos.Y = chunk.Position.Y;
                    newpos.Z = chunk.Position.Z - Chunk.WIDTH;
                    //Create a new chunk
                    neighborchunk = new Chunk(this, ref neighborcoords, ref newpos);
                    //Update flag and connect event chain to neighbor
                    chunk.NeighborsInMemoryFlag |= 64;
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
                    //Add new chunk to manager
                    AddChunk(neighborchunk);
                }
                else if (contains)//Chunk is already in memory connect to it
                {
                    //Update flag and connect event chain to neighbor
                    chunk.ConsolidateFlags(neighborchunk, 64);
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
                }
            }
            //-x -z
            //Get new chunkcoords
            chunk.Coords.Add(-1, -1, out neighborcoords);
            contains = ChunkStorage.Contains(ref neighborcoords, out neighborchunk);
            if ((chunk.NeighborsInMemoryFlag & 128) != 128)
            {
                if (!contains)
                {
                    //Shift this chunks position to get the new chunks relative position
                    newpos.X = chunk.Position.X - Chunk.WIDTH;
                    newpos.Y = chunk.Position.Y;
                    newpos.Z = chunk.Position.Z - Chunk.WIDTH;
                    //Create a new chunk
                    neighborchunk = new Chunk(this, ref neighborcoords, ref newpos);
                    //Update flag and connect event chain to neighbor
                    chunk.NeighborsInMemoryFlag |= 128;
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
                    //Add new chunk to manager
                    AddChunk(neighborchunk);
                }
                else if (contains) //Chunk is already in memory connect to it
                {
                    //Update flag and connect event chain to neighbor
                    chunk.ConsolidateFlags(neighborchunk, 128);
                    neighborchunk.StateChanged += chunk.NeighborChangedStateCallback;
                    chunk.StateChanged += neighborchunk.NeighborChangedStateCallback;
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

            while (_loadThreadContinue)
            {
                Monitor.Enter(_loadQueue);
                while (_loadQueue.Count == 0) Monitor.Wait(_loadQueue, 10);
                Monitor.Exit(_loadQueue);

                if(_loadQueue.Count > PER_TICK_CHUNKS_LOAD) _loadQueue.Sort((x, y) => x.Coords.CompareDistance(ref y.Coords, ref PlayerPosition));

                while (_loadQueue.Count > 0 && _numToLoad >0)
                {
                    curr = _loadQueue[0];
                    if (curr.State == ChunkState.Unloading)
                    {
                        _loadQueue.RemoveAt(0);
                        continue;
                    }

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
                    curr.ChangeState(this, ChunkState.Loaded);
                    _loadQueue.RemoveAt(0);
                    _numToLoad--;
                }
            }
            

            
        }

        public void LightChunks(Object threadContext)
        {
            

            Chunk curr;

            while (_lightThreadContinue)
            {
                Monitor.Enter(LightQueue);
                Monitor.Wait(LightQueue, 15);
                Monitor.Exit(LightQueue);

                if(LightQueue.Count > PER_TICK_CHUNKS_LIGHT) LightQueue.Sort((x, y) => x.Coords.CompareDistance(ref y.Coords, ref PlayerPosition));

                while (LightQueue.Count > 0 && _numToLight>0)
                {
                    curr = LightQueue[0];
                    if (curr.State == ChunkState.Unloading)
                    {
                        LightQueue.RemoveAt(0);
                        continue;
                    }

                    curr.PropagateSun(CubeStorage);

                    curr.ChangeState(this, ChunkState.PendingBuild);
                    LightQueue.RemoveAt(0);
                    _numToLight--;
                }
            }
        }

        public void BuildChunkVertices(Object threadContext)
        { 
            Chunk curr;
            while (_buildThreadContinue)
            {
                Monitor.Enter(BuildQueue);
                Monitor.Wait(BuildQueue, 25);
                Monitor.Exit(BuildQueue);

                if(BuildQueue.Count > PER_TICK_CHUNKS_BUILD) BuildQueue.Sort((x, y) => x.Coords.CompareDistance(ref y.Coords, ref PlayerPosition));

                while (BuildQueue.Count > 0 && _numToBuild>0)
                {
                    curr = BuildQueue[0];
                    if (curr.State == ChunkState.Unloading)
                    {
                        BuildQueue.RemoveAt(0);
                        continue;
                    }

                    curr.BuildVertices(_vertexBuffer, _graphics, CubeStorage);

                    if (curr.Meshes.Count > 0 && !DrawList.Contains(curr)) DrawList.Add(curr);
                    curr.ChangeState(this, ChunkState.Built);
                    BuildQueue.RemoveAt(0);
                    _numToBuild--;
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
            chunk.ChangeState(this, ChunkState.Unloading);
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

            _unloadThreadContinue = true;
        }

        public void PrintStats()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("--Loaded--");
            for (int i = 0; i < ChunkStorage.LoadedChunks.Count; i++)
            {
                builder.AppendLine(ChunkStorage.LoadedChunks[i].ToString());
            }
            
            builder.AppendLine();
            builder.AppendLine("light: " + LightQueue.Count.ToString() + "|build: " + BuildQueue.Count.ToString() + "|rebuild: " + RebuildQueue.Count.ToString());
            builder.AppendLine("draw: " + DrawList.Count.ToString() + "|unload: " + UnloadQueue.Count.ToString());
            log.Write("ManagerStats", "--Everything Else--", builder.ToString());
        }
    }
}
