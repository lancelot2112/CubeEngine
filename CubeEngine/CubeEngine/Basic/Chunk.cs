using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CubeEngine.Rendering;
using System.Threading;

namespace CubeEngine.Basic
{
    using log = XerUtilities.Debugging.Logger;

    public enum ChunkState
    {
        PendingLoad,
        Loading,
        Loaded,
        PendingLight,
        Lighting,
        PendingBuild,
        Building,
        Built,
        PendingRebuild,
        Unloading,
    }

    /// <summary>
    /// A collection of cubes stored in a 3d array along with a mesh for rendering the terrain.
    /// </summary>
    public class Chunk
    {
        public static int ObjectCount = 0;
        public const int WIDTH = 16;
        public const int HEIGHT = 128;
        public const int DEPENDENCIES_MET_FLAG_VALUE = 255;

        /// <summary>
        ///  1: +x
        ///  2: -x
        ///  4: +z
        ///  8: -z
        ///  16: +x +z
        ///  32: -x +z
        ///  64: +x -z
        ///  128: -x -z
        /// </summary>
        public byte NeighborsInMemoryFlag;
        public byte LightDependenciesFlag;
        public byte BuildDependenciesFlag;
        public int Xstart;
        public int Zstart;
        public int ObjectNumber;

        public ChunkCoords Coords;
        public Vector3 Position;

        private ChunkState _state;
        public ChunkState State { get { return _state; } }
        public bool ChangedSinceLoad;
        public bool Empty;

        public List<ChunkSubMesh> Meshes;
        private int[,] _heightMap;

        public delegate void ChangedStateHandler(ChunkManager manager, Chunk chunk, ChunkState state);
        public event ChangedStateHandler StateChanged;
        

        public Chunk(ChunkManager manager, ref ChunkCoords coords, ref Vector3 initialPosition)
        {            
            ObjectCount += 1;
            ObjectNumber = ObjectCount;
            Position = initialPosition;
            Coords = coords;
            _heightMap = new int[WIDTH, WIDTH];
            Meshes = new List<ChunkSubMesh>();

            Xstart = coords.X * Chunk.WIDTH;
            Zstart = coords.Z * Chunk.WIDTH;
            manager.CubeStorage.WrapCoords(ref Xstart, ref Zstart);
            
            ChangedSinceLoad = false;
            Empty = true;
        }

        public void Update(float dt, Vector3 deltaPosition)
        {
            Position -= deltaPosition;
        }

        public void ChangeState(ChunkManager manager, ChunkState newState)
        {
            log.Write(newState.ToString(), this.ToString(), "");
            if (StateChanged != null) StateChanged(manager, this, newState);
            _state = newState;
        }        

        public void ConsolidateFlags(Chunk chunk, byte neighbors)
        {
            if (chunk.State >= ChunkState.Loading) NeighborsInMemoryFlag |= neighbors;
            if (chunk.State >= ChunkState.Loaded) LightDependenciesFlag |= neighbors;
            if (chunk.State >= ChunkState.PendingBuild) BuildDependenciesFlag |= neighbors;
        }

        public void NeighborChangedStateCallback(ChunkManager manager, Chunk chunk, ChunkState state)
        {
            switch (state)
            {
                case ChunkState.Loaded:
                    {
                        LightDependenciesFlag |= Coords.Neighbors(ref chunk.Coords);
                        break;
                    }
                case ChunkState.PendingLight:
                    break;
                case ChunkState.Lighting:
                    break;
                case ChunkState.PendingBuild:
                    {
                        BuildDependenciesFlag |= Coords.Neighbors(ref chunk.Coords);
                        break;
                    }
                case ChunkState.Built:
                    break;
                case ChunkState.PendingRebuild:
                    break;
                case ChunkState.Unloading:
                    {
                        byte val = Coords.Neighbors(ref chunk.Coords);
                        if((NeighborsInMemoryFlag & val) == val) NeighborsInMemoryFlag -= val;
                        if((LightDependenciesFlag & val) == val) LightDependenciesFlag -= val;
                        if((BuildDependenciesFlag & val) == val) BuildDependenciesFlag -= val;
                        break;
                    }
            }
        }

        public void PropagateSun(CubeStorage store)
        {
            int maxIndex = HEIGHT-1;

            int sun;
            int posXsun;
            int negXsun;
            int posZsun;
            int negZsun;
            int attenuated;

            Cube curr;
            int startX = WIDTH * Coords.X;
            int startZ = WIDTH * Coords.Z;


            //SEED sunlight in the top layer
            for (int x = startX; x < startX + WIDTH; x++)
            {
                for (int z = startZ; z < startZ + WIDTH; z++)
                {
                    store.SetSunlight(x, maxIndex, z, 15);                    
                }
            }

            maxIndex = WIDTH-1;

            for (int y = HEIGHT - 2; y >= 0; y--)
            {
                for (int x = startX; x < startX + WIDTH; x++)
                {
                    for (int z = startZ; z < startZ + WIDTH; z++)
                    {
                        store.GetCube(x, y, z, out curr);
                        if (!curr.IsTransparent) continue;

                        sun = store.GetSunlight(x, y + 1, z);
                        posXsun = store.GetSunlight(x + 1, y, z) - 1;
                        if (sun < posXsun) sun = posXsun;
                        negXsun = store.GetSunlight(x - 1, y, z) - 1;
                        if (sun < negXsun) sun = negXsun;
                        posZsun = store.GetSunlight(x, y, z + 1) - 1;
                        if (sun < posZsun) sun = posZsun;
                        negZsun = store.GetSunlight(x, y, z - 1) - 1;
                        if (sun < negZsun) sun = negZsun;
                        if (sun > curr.SunLight) store.SetSunlight(x, y, z, sun);

                        attenuated = sun - 1;
                        if (posXsun + 1 < attenuated && x == startX + maxIndex) store.SetSunlight(x + 1, y, z, attenuated);
                        else if (negXsun + 1 < attenuated && x == startX) store.SetSunlight(x - 1, y, z, attenuated);
                        if (posZsun + 1 < attenuated && z == startZ + maxIndex) store.SetSunlight(x, y, z + 1, attenuated);
                        else if (negZsun + 1 < attenuated && z == startZ) store.SetSunlight(x, y, z - 1, attenuated);
                    }
                }
            }
        }
        public void BuildVertices(CubeVertex[] buffer, GraphicsDevice graphics, CubeStorage store)
        {
            ChunkSubMesh currentMesh;
            int i = 0;
            while(i < HEIGHT)
            {
                currentMesh = new ChunkSubMesh(i, ref Position);
                currentMesh.BuildVertices(buffer, graphics, this, store);
                if (!currentMesh.Empty) Meshes.Add(currentMesh);
                i += Chunk.WIDTH;
            }

        }

        public bool LoadFromDisk()
        {
            return false;
        }

        public void SaveToDisk()
        {
        }

        public void Dispose()
        {
            for (int i = 0; i < Meshes.Count; i++)
            {
                Meshes[i].Dispose();
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(Coords.ToString());
            builder.AppendLine("loF: " + NeighborsInMemoryFlag.ToString() +"|liF: " + LightDependenciesFlag.ToString() + "|buF: " + BuildDependenciesFlag.ToString());
            builder.AppendLine("state: " + State.ToString() + "|empty: " + Empty.ToString() + "|changed: " + ChangedSinceLoad.ToString());
            return builder.ToString();
        }
    }


}
