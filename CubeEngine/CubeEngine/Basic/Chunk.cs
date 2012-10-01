using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CubeEngine.Rendering;

namespace CubeEngine.Basic
{
    /// <summary>
    /// A collection of cubes stored in a 3d array along with a mesh for rendering the terrain.
    /// </summary>
    public class Chunk
    {
        public static int ObjectCount = 0;
        public const int WIDTH = 32;
        public const int HEIGHT = 128;
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
        public int XIndex;
        public int ZIndex;
        public int ObjectNumber;

        public ChunkCoords Coords;
        public Vector3 LocalPosition;

        public bool Loaded;
        public bool Lighted;
        public volatile bool CompletedInitialBuild;
        public bool ChangedSinceLoad;
        public bool Empty;

        public List<ChunkSubMesh> Meshes;

        private Cube[, ,] m_cubes;
        private int[,] m_heightMap;
        

        public Chunk(ChunkManager manager, ChunkCoords coords)
        {            
            ObjectCount += 1;
            ObjectNumber = ObjectCount;
            manager.ChunkLoadingEvent += ChunkLoadingCallback;
            manager.ChunkLoadedEvent += ChunkLoadedCallback;
            manager.ChunkLitEvent += ChunkLitCallback;
            Coords = coords;
            LocalPosition = new Vector3(coords.X * WIDTH, 0f, coords.Z * WIDTH);
            m_cubes = new Cube[WIDTH, HEIGHT, WIDTH];
            m_heightMap = new int[WIDTH, WIDTH];
            Meshes = new List<ChunkSubMesh>();

            ChangedSinceLoad = false;
            CompletedInitialBuild = false;
            Empty = true;
        }

        public void Update(float dt, Vector3 deltaPosition)
        {
            if (CompletedInitialBuild)
            {
                LocalPosition -= deltaPosition;

                for (int i = 0; i < Meshes.Count; i++)
                {
                    Meshes[i].Update(LocalPosition);
                }
            }
        }

        public void SetCube(int x, int y, int z, ref Cube cube)
        {
            if (InChunk(x, y, z)) m_cubes[x, y, z] = cube;
        }

        public bool GetCube(int x, int y, int z, out Cube cube)
        {
            if (InChunk(x, y, z))
            {
                cube = m_cubes[x, y, z];
                return true;
            }
            else
            {
                cube = Cube.NULL;
                return false;
            }
        }

        public Cube GetCube(int x, int y, int z)
        {
            return m_cubes[x, y, z];
        }

        public int GetSunLight(int x, int y, int z)
        {
            return m_cubes[x, y, z].SunLight;
        }

        public void SetSunLight(int x, int y, int z, int sunLight)
        {
            m_cubes[x, y, z].SunLight = sunLight;
            Lighted = false;
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

        public bool InChunk(int x, int y, int z)
        {

            if (x < 0 || x >= WIDTH)
                return false;
            if (y < 0 || y >= HEIGHT)
                return false;
            if (z < 0 || z >= WIDTH)
                return false;

            return true;
        }

        public void PropagateSun(Chunk posX, Chunk negX, Chunk posZ, Chunk negZ)
        {
            int maxIndex = HEIGHT-1;

            int sun;
            int currSun;

            int posXsun;
            int negXsun;
            int posZsun;
            int negZsun;

            int attenuated;

            //SEED sunlight in the top layer
            for (int x = 0; x < WIDTH; x++)
            {
                for (int z = 0; z < WIDTH; z++)
                {
                    m_cubes[x, maxIndex, z].SunLight = 15 - m_cubes[x, maxIndex, z].Attenuation();
                }
            }

            maxIndex = WIDTH-1;
            for (int y = HEIGHT - 2; y >= 0; y--)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    for (int z = 0; z < WIDTH; z++)
                    {
                        if (!m_cubes[x, y, z].IsTransparent()) continue;

                        sun = m_cubes[x, y + 1, z].SunLight;
                        posXsun = ((x == maxIndex) ? posX.GetSunLight(0, y, z) : m_cubes[x + 1, y, z].SunLight) - 1;
                        sun = (sun > posXsun) ? sun : posXsun;
                        negXsun = ((x == 0) ? negX.GetSunLight(maxIndex, y, z) : m_cubes[x - 1, y, z].SunLight) - 1;
                        sun = (sun > negXsun) ? sun : negXsun;
                        posZsun = ((z == maxIndex) ? posZ.GetSunLight(x, y, 0) : m_cubes[x, y, z + 1].SunLight) - 1;
                        sun = (sun > posZsun) ? sun : posZsun;
                        negZsun = ((z == 0) ? negZ.GetSunLight(x, y, maxIndex) : m_cubes[x, y, z - 1].SunLight) - 1;
                        sun = (sun > negZsun) ? sun : negZsun;
                        currSun = m_cubes[x,y,z].SunLight;
                        m_cubes[x, y, z].SunLight = (currSun > sun) ? currSun : sun;

                        attenuated = sun - 1;
                        if (posXsun < attenuated && x == maxIndex) posX.SetSunLight(0, y, z, attenuated);
                        else if (negXsun < attenuated && x == 0) negX.SetSunLight(maxIndex, y, z, attenuated);
                        if (posZsun < attenuated && z == maxIndex) posZ.SetSunLight(x, y, 0, attenuated);
                        else if (negZsun < attenuated && z == 0) negZ.SetSunLight(x, y, maxIndex, attenuated);
                    }
                }
            }

            Lighted = true;
        }
        public void BuildVertices(List<CubeVertex> buffer, GraphicsDevice graphics, Chunk posX, Chunk negX, Chunk posZ, Chunk negZ)
        {           

            ChunkSubMesh currentMesh;
            int i = 0;
            while(i < HEIGHT)
            {
                currentMesh = new ChunkSubMesh(i);
                currentMesh.BuildVertices(buffer, graphics, m_cubes, posX, negX, posZ, negZ);
                if (!currentMesh.Empty) Meshes.Add(currentMesh);
                i += Chunk.WIDTH;
            }

            if (!CompletedInitialBuild)
            {
                if (posX.CompletedInitialBuild) LocalPosition = posX.LocalPosition - Vector3.Right * WIDTH;
                else if (posZ.CompletedInitialBuild) LocalPosition = posZ.LocalPosition - Vector3.Backward * WIDTH;
                else if (negX.CompletedInitialBuild) LocalPosition = negX.LocalPosition + Vector3.Right * WIDTH;
                else if (negZ.CompletedInitialBuild) LocalPosition = negZ.LocalPosition + Vector3.Backward * WIDTH;
                CompletedInitialBuild = true;
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
            builder.AppendLine(Coords.ToString() + "{" + XIndex + "," + ZIndex + "}");
            builder.AppendLine("loF: " + LoadDependenciesFlag.ToString() +"|liF: " + LightDependenciesFlag.ToString() + "|buF: " + BuildDependenciesFlag.ToString());
            builder.AppendLine("load: " + Loaded.ToString() + "|lit: " + Lighted.ToString() + "|initBuild: " + CompletedInitialBuild.ToString() + "|empty: " + Empty.ToString() + "|changed: " + ChangedSinceLoad.ToString());
            return builder.ToString();
        }
    }


}
