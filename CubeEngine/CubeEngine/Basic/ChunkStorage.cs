using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CubeEngine.Basic
{
    public class ChunkStorage
    {
        public Chunk[,] Chunks;

        public List<Chunk> LoadedChunks;
        public int LoadedChunkCount { get { return LoadedChunks.Count; } }
        public bool ChunksAddedSinceSort;

        private int _dimMask;

        public ChunkStorage(int len)
        {
            bool lenp2 = (len != 0) && ((len & (len - 1)) == 0);
            if (!lenp2) throw new NotSupportedException("len is not power of 2.");

            _dimMask = len - 1;            

            Chunks = new Chunk[len, len];
            LoadedChunks = new List<Chunk>();
        }

        public void Store(Chunk chunk, ChunkManager manager)
        {
            int x = chunk.Coords.X & _dimMask;
            int z = chunk.Coords.Z & _dimMask;

            if (Chunks[x, z] != null)
            {               
                LoadedChunks.Remove(Chunks[x, z]);
                manager.PrepareChunkForUnload(Chunks[x, z]);
            }

            Chunks[x, z] = chunk;

            LoadedChunks.Add(chunk);
            ChunksAddedSinceSort = true;
        }

        public void UpdateChunks(float dt, Vector3 deltaPosition)
        {
            //TODO: Make sure entire list gets updated upon removal from an off thread
            for (int i = 0; i < LoadedChunks.Count; i++)
            {
                LoadedChunks[i].Update(dt, deltaPosition);
            }
        }
        public Chunk GetChunk(int x, int z)
        {
            x = x & _dimMask;
            z = z & _dimMask;

            return Chunks[x, z];
        }

        public bool Contains(ref ChunkCoordinate coords, out Chunk chunk)
        {
            int x = coords.X & _dimMask;
            int z = coords.Z & _dimMask;

            chunk = Chunks[x, z];
            return (chunk != null) ? chunk.Coords.Equals(ref coords) : false;
        }

        public bool Contains(Chunk chunk)
        {
            return LoadedChunks.Contains(chunk);
        }

        public void WrapIndex(ref int ind)
        {
            ind &= _dimMask;
        }

        public void UnloadAll(ChunkManager manager)
        {
            for (int i = 0; i < LoadedChunks.Count; i++)
            {
                manager.PrepareChunkForUnload(LoadedChunks[i]);
            }
            LoadedChunks.Clear();

            for (int x = 0; x <= _dimMask; x++)
            {
                for (int z = 0; z <= _dimMask; z++)
                {
                    Chunks[x, z] = null;
                }
            }
        }
    }
}
