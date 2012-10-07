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
    /// <summary>
    /// A collection of cubes stored in a 3d array along with a mesh for rendering the terrain.
    /// </summary>
    public class Chunk
    {
        public static int ObjectCount = 0;
        public const int WIDTH = 16;
        public const int HEIGHT = 256;
        public const int DEPENDENCIES_MET_FLAG_VALUE = 15;

        /// <summary>
        ///  1: +x
        ///  2: -x
        ///  4: +z
        ///  8: -z
        /// </summary>
        public byte LoadDependenciesFlag;
        public byte LightDependenciesFlag;
        public byte BuildDependenciesFlag;
        public int Xstart;
        public int Zstart;
        public int ObjectNumber;

        public ChunkCoords Coords;
        public Vector3 Position;

        public bool Loaded;
        public bool Lit;        
        public bool Empty;
        public bool Unloading;
        public bool ChangedSinceLoad;

        public List<ChunkSubMesh> Meshes;
        private int[,] _heightMap;
        

        public Chunk(ChunkManager manager, ref ChunkCoords coords, ref Vector3 initialPosition)
        {            
            ObjectCount += 1;
            ObjectNumber = ObjectCount;
            manager.ChunkLoadingEvent += ChunkLoadingCallback;
            manager.ChunkLoadedEvent += ChunkLoadedCallback;
            manager.ChunkLitEvent += ChunkLitCallback;
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

        public void ChunkLoadingCallback(ChunkManager manager, Chunk chunk)
        {
            LoadDependenciesFlag |= Coords.Neighbors(ref chunk.Coords);
        }

        public void ChunkLoadedCallback(ChunkManager manager, Chunk chunk)
        {
            LightDependenciesFlag |= Coords.Neighbors(ref chunk.Coords);
        }

        public void ChunkLitCallback(ChunkManager manager, Chunk chunk)
        {
            BuildDependenciesFlag |= Coords.Neighbors(ref chunk.Coords);
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

            Lit = true;
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
            builder.AppendLine("loF: " + LoadDependenciesFlag.ToString() +"|liF: " + LightDependenciesFlag.ToString() + "|buF: " + BuildDependenciesFlag.ToString());
            builder.AppendLine("load: " + Loaded.ToString() + "|lit: " + Lit.ToString() + "|empty: " + Empty.ToString() + "|changed: " + ChangedSinceLoad.ToString());
            return builder.ToString();
        }
    }


}
