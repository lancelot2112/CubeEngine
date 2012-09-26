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
        public const int DEPENDENCIES_MET_FLAG = 15;

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
        public bool Built;
        public bool Empty;

        private Cube[, ,] m_cubes;
        

        public Chunk(ChunkManager manager, ChunkCoords coords)
        {
            manager.ChunkLoadedEvent += CheckDependence;
            Coords = coords;
            LocalPosition = new Vector3(coords.X * WIDTH, 0f, coords.Z * WIDTH);
            m_cubes = new Cube[WIDTH, HEIGHT, WIDTH];
            Meshes = new List<ChunkSubMesh>();

            ChangedSinceLoad = false;
            Built = false;
            Empty = true;
            PositionUpdated = false;
        }

        public void Update(float dt, Vector3 deltaPosition)
        {
            LocalPosition -= deltaPosition;
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

        public void CheckDependence(ChunkManager manager, Chunk chunk)
        {
            if (!Built)
            {
                DependenciesFlag |= Coords.Neighbors(chunk.Coords);
                if (DependenciesFlag == 15 && !manager.ChunksToDraw.Contains(this) && !Built) manager.ChunksToBuild.Enqueue(this);
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

        public void BuildVertices(GraphicsDevice graphics, Chunk posX, Chunk negX, Chunk posZ, Chunk negZ)
        {
            ChunkSubMesh currentMesh;
            int i = 0;
            while(i < HEIGHT)
            {
                if (posX.PositionUpdated) LocalPosition = posX.LocalPosition - Vector3.Right * WIDTH;
                else if (posZ.PositionUpdated) LocalPosition = posZ.LocalPosition - Vector3.Forward * WIDTH;
                else if (negX.PositionUpdated) LocalPosition = negX.LocalPosition + Vector3.Right * WIDTH;
                else if (negZ.PositionUpdated) LocalPosition = negZ.LocalPosition + Vector3.Forward * WIDTH;
                PositionUpdated = true;

                currentMesh = new ChunkSubMesh(i);
                currentMesh.BuildVertices(graphics, m_cubes, posX, negX, posZ, negZ);
                if (!currentMesh.Empty) Meshes.Add(currentMesh);
                i += ChunkSubMesh.SIZE_Z;
            }

            Built = true;
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
