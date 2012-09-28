using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CubeEngine.Basic
{
    /// <summary>
    /// A collection of cubes stored in a 3d array along with a mesh for rendering the terrain.
    /// </summary>
    public class Chunk
    {
        public const int WIDTH = 16;
        public const int HEIGHT = 128;
        public const int DEPENDENCIES_MET_FLAG_VALUE = 15;

        /// <summary>
        ///  1: +x
        ///  2: -x
        ///  4: +z
        ///  8: -z
        /// </summary>
        public byte DependenciesFlag;
        public int XIndex;
        public int ZIndex;

        public ChunkCoords Coords;
        public Vector3 LocalPosition;
        public bool PositionUpdated;

        public List<ChunkSubMesh> Meshes;
        public bool ChangedSinceLoad;
        public volatile bool CompletedInitialBuild;
        public bool Empty;

        private Cube[, ,] m_cubes;
        

        public Chunk(ChunkManager manager, ChunkCoords coords)
        {
            manager.ChunkLoadedEvent += ChunkLoadedCallback;
            Coords = coords;
            LocalPosition = new Vector3(coords.X * WIDTH, 0f, coords.Z * WIDTH);
            m_cubes = new Cube[WIDTH, HEIGHT, WIDTH];
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
                cube = Cube.AIR;
                return false;
            }
        }

        public Cube GetCube(int x, int y, int z)
        {
            if (InChunk(x, y, z)) return m_cubes[x, y, z];
            else return Cube.AIR;
        }

        public void ChunkLoadedCallback(ChunkManager manager, Chunk chunk)
        {
            if (!CompletedInitialBuild)
            {
                DependenciesFlag |= Coords.Neighbors(chunk.Coords);
            }
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

        public void BuildVertices(List<VertexPositionColor> buffer, GraphicsDevice graphics, Chunk posX, Chunk negX, Chunk posZ, Chunk negZ)
        {         

            ChunkSubMesh currentMesh;
            int i = 0;
            while(i < HEIGHT)
            {
                currentMesh = new ChunkSubMesh(i);
                currentMesh.BuildVertices(buffer, graphics, m_cubes, posX, negX, posZ, negZ);
                if (!currentMesh.Empty) Meshes.Add(currentMesh);
                i += ChunkSubMesh.SIZE_Z;
            }

            if (!CompletedInitialBuild)
            {
                if (posX.CompletedInitialBuild) LocalPosition = posX.LocalPosition - Vector3.Right * WIDTH;
                else if (posZ.CompletedInitialBuild) LocalPosition = posZ.LocalPosition - Vector3.Forward * WIDTH;
                else if (negX.CompletedInitialBuild) LocalPosition = negX.LocalPosition + Vector3.Right * WIDTH;
                else if (negZ.CompletedInitialBuild) LocalPosition = negZ.LocalPosition + Vector3.Forward * WIDTH;
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
    }


}
