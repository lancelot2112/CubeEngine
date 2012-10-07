using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CubeEngine.Rendering;



namespace CubeEngine.Basic
{
    using log = XerUtilities.Debugging.Logger;

    public class ChunkSubMesh
    {
        

        private int _yStartIndex;
        private int _yEndIndex;
        public Vector3 Offset;
        public Vector3 Position;

        //Statisitics
        public int RenderableBlocks;
        public int SidesRenderable;

        //Render Members
        public bool AllowRender;
        public bool Empty;
        public bool SideCountNeeded;
        public VertexBuffer VertexBuffer;

        public ChunkSubMesh(int yStartIndex, ref Vector3 chunkPosition)
        {
            _yStartIndex = yStartIndex;
            _yEndIndex = yStartIndex + Chunk.WIDTH - 1;

            Offset = Vector3.Up * yStartIndex;
            Vector3.Add(ref Offset, ref chunkPosition, out Position);

            SideCountNeeded = true;
        }

        public void GetBoundingBox(out BoundingBox boundingBox)
        {
            boundingBox.Min = Position;  
            boundingBox.Max = boundingBox.Min + new Vector3(Chunk.WIDTH, Chunk.WIDTH, Chunk.WIDTH);                      
        }

        public void Update(Vector3 chunkPosition)
        {
            Vector3.Add(ref chunkPosition, ref Offset, out Position);
        }

        public void BuildVertices(CubeVertex[] vertexBuffer, GraphicsDevice graphics, Chunk parent, CubeStorage store)
        {
            if (SideCountNeeded) CollectSubmeshStats(parent, store);
            if (vertexBuffer.Length < SidesRenderable * 6f) vertexBuffer = new CubeVertex[(int)(SidesRenderable * 6.1f)];
            
            Cube current;
            Cube n0;
            Cube n1;
            Cube n2;
            Cube n3;
            Cube n4;
            Cube n5;
            Cube n6;
            Cube n7;
            Cube n8;

            Vector3 offset;
            Vector3 posNNN;
            Vector3 posNNP;
            Vector3 posNPN;
            Vector3 posNPP;
            Vector3 posPNN;
            Vector3 posPNP;
            Vector3 posPPN;
            Vector3 posPPP;

            int ind = 0;
            int worldX = 0;
            int worldZ = 0;
            int reg=0;

            int vert1light = 0;
            int vert2light = 0;
            int vert3light = 0;
            int vert4light = 0;

            int maxY = Chunk.HEIGHT - 1;

            for (int x = 0; x < Chunk.WIDTH; x++)
            {
                worldX = x + parent.Coords.X * Chunk.WIDTH;
                for (int z = 0; z < Chunk.WIDTH; z++)
                {
                    worldZ = z + parent.Coords.Z * Chunk.WIDTH;
                    for (int y = _yStartIndex; y <= _yEndIndex; y++)
                    {
                        store.GetCube(worldX, y, worldZ, out current);

                        //log.Write("worldCoords", "{" + worldX.ToString() + "," + y.ToString() + "," + worldZ.ToString() + "}", "");

                        if (!current.IsRenderable) continue;

                        offset.X = x;
                        offset.Y = y - _yStartIndex;
                        offset.Z = z;

                        Vector3.Add(ref CubeVertex.CORNER_NNN, ref offset, out posNNN);
                        Vector3.Add(ref CubeVertex.CORNER_NNP, ref offset, out posNNP);
                        Vector3.Add(ref CubeVertex.CORNER_NPN, ref offset, out posNPN);
                        Vector3.Add(ref CubeVertex.CORNER_NPP, ref offset, out posNPP);
                        Vector3.Add(ref CubeVertex.CORNER_PNN, ref offset, out posPNN);
                        Vector3.Add(ref CubeVertex.CORNER_PNP, ref offset, out posPNP);
                        Vector3.Add(ref CubeVertex.CORNER_PPN, ref offset, out posPPN);
                        Vector3.Add(ref CubeVertex.CORNER_PPP, ref offset, out posPPP);

                        //-x
                        reg = worldX - 1;
                        store.GetCube(reg, y, worldZ, out n0);

                        if (n0.IsTransparent)
                        {
                            store.SafeGetCube(reg, y + 1, worldZ, out n1);
                            store.SafeGetCube(reg, y + 1, worldZ + 1, out n2);
                            store.GetCube(reg, y, worldZ + 1, out n3);
                            store.SafeGetCube(reg, y - 1, worldZ + 1, out n4);
                            store.SafeGetCube(reg, y - 1, worldZ, out n5);
                            store.SafeGetCube(reg, y - 1, worldZ - 1, out n6);
                            store.GetCube(reg, y, worldZ - 1, out n7);
                            store.SafeGetCube(reg, y + 1, worldZ - 1, out n8);

                            vertexBuffer[ind++] = new CubeVertex(ref posNPN, ref CubeVertex.N_NEG_X, ref CubeVertex.TC00, ref current, ref n0, ref n1, ref n7, ref n8);
                            vertexBuffer[ind++] = new CubeVertex(ref posNPP, ref CubeVertex.N_NEG_X, ref CubeVertex.TC10, ref current, ref n0, ref n1, ref n2, ref n3);
                            vertexBuffer[ind++] = new CubeVertex(ref posNNN, ref CubeVertex.N_NEG_X, ref CubeVertex.TC01, ref current, ref n0, ref n5, ref n6, ref n7);
                            vertexBuffer[ind++] = new CubeVertex(ref posNNN, ref CubeVertex.N_NEG_X, ref CubeVertex.TC01, ref current, ref n0, ref n5, ref n6, ref n7);
                            vertexBuffer[ind++] = new CubeVertex(ref posNPP, ref CubeVertex.N_NEG_X, ref CubeVertex.TC10, ref current, ref n0, ref n1, ref n2, ref n3);
                            vertexBuffer[ind++] = new CubeVertex(ref posNNP, ref CubeVertex.N_NEG_X, ref CubeVertex.TC11, ref current, ref n0, ref n3, ref n4, ref n5);
                        }

                        //+x
                        reg = worldX + 1;
                        store.GetCube(reg, y, worldZ, out n0);

                        if (n0.IsTransparent)
                        {
                            store.SafeGetCube(reg, y + 1, worldZ, out n1);
                            store.SafeGetCube(reg, y + 1, worldZ + 1, out n2);
                            store.GetCube(reg, y, worldZ + 1, out n3);
                            store.SafeGetCube(reg, y - 1, worldZ + 1, out n4);
                            store.SafeGetCube(reg, y - 1, worldZ, out n5);
                            store.SafeGetCube(reg, y - 1, worldZ - 1, out n6);
                            store.GetCube(reg, y, worldZ - 1, out n7);
                            store.SafeGetCube(reg, y + 1, worldZ - 1, out n8);

                            vertexBuffer[ind++] = new CubeVertex(ref posPPP, ref CubeVertex.N_POS_X, ref CubeVertex.TC00, ref current, ref n0, ref n1, ref n2, ref n3);
                            vertexBuffer[ind++] = new CubeVertex(ref posPPN, ref CubeVertex.N_POS_X, ref CubeVertex.TC10, ref current, ref n0, ref n1, ref n7, ref n8);
                            vertexBuffer[ind++] = new CubeVertex(ref posPNP, ref CubeVertex.N_POS_X, ref CubeVertex.TC01, ref current, ref n0, ref n3, ref n4, ref n5);
                            vertexBuffer[ind++] = new CubeVertex(ref posPNP, ref CubeVertex.N_POS_X, ref CubeVertex.TC01, ref current, ref n0, ref n3, ref n4, ref n5);
                            vertexBuffer[ind++] = new CubeVertex(ref posPPN, ref CubeVertex.N_POS_X, ref CubeVertex.TC10, ref current, ref n0, ref n1, ref n7, ref n8);
                            vertexBuffer[ind++] = new CubeVertex(ref posPNN, ref CubeVertex.N_POS_X, ref CubeVertex.TC11, ref current, ref n0, ref n5, ref n6, ref n7);
                        }

                        //-y
                        reg = y - 1;
                        if (y != 0) store.GetCube(worldX, reg, worldZ, out n0);
                        else n0 = Cube.NULL;

                        if (n0.IsTransparent && n0.Type != CubeType.NULL)
                        {
                            store.GetCube(worldX + 1, reg, worldZ, out n1);
                            store.GetCube(worldX + 1, reg, worldZ + 1, out n2);
                            store.GetCube(worldX, reg, worldZ + 1, out n3);
                            store.GetCube(worldX - 1, reg, worldZ + 1, out n4);
                            store.GetCube(worldX - 1, reg, worldZ, out n5);
                            store.GetCube(worldX - 1, reg, worldZ - 1, out n6);
                            store.GetCube(worldX, reg, worldZ - 1, out n7);
                            store.GetCube(worldX + 1, reg, worldZ - 1, out n8);

                            vertexBuffer[ind++] = new CubeVertex(ref posNNP, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC00, ref current, ref n0, ref n3, ref n4, ref n5);
                            vertexBuffer[ind++] = new CubeVertex(ref posPNP, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC10, ref current, ref n0, ref n1, ref n2, ref n3);
                            vertexBuffer[ind++] = new CubeVertex(ref posNNN, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC01, ref current, ref n0, ref n5, ref n6, ref n7);
                            vertexBuffer[ind++] = new CubeVertex(ref posNNN, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC01, ref current, ref n0, ref n5, ref n6, ref n7);
                            vertexBuffer[ind++] = new CubeVertex(ref posPNP, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC10, ref current, ref n0, ref n1, ref n2, ref n3);
                            vertexBuffer[ind++] = new CubeVertex(ref posPNN, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC11, ref current, ref n0, ref n1, ref n7, ref n8);
                        }

                        //+y
                        reg = y + 1;
                        if (y != maxY) store.GetCube(worldX, reg, worldZ, out n0);
                        else n0 = Cube.NULL;

                        if (n0.IsTransparent && n0.Type != CubeType.NULL)
                        {
                            store.GetCube(worldX + 1, reg, worldZ, out n1);
                            store.GetCube(worldX + 1, reg, worldZ + 1, out n2);
                            store.GetCube(worldX, reg, worldZ + 1, out n3);
                            store.GetCube(worldX - 1, reg, worldZ + 1, out n4);
                            store.GetCube(worldX - 1, reg, worldZ, out n5);
                            store.GetCube(worldX - 1, reg, worldZ - 1, out n6);
                            store.GetCube(worldX, reg, worldZ - 1, out n7);
                            store.GetCube(worldX + 1, reg, worldZ - 1, out n8);

                            vertexBuffer[ind++] = new CubeVertex(ref posNPN, ref CubeVertex.N_POS_Y, ref CubeVertex.TC00, ref current, ref n0, ref n5, ref n6, ref n7);
                            vertexBuffer[ind++] = new CubeVertex(ref posPPN, ref CubeVertex.N_POS_Y, ref CubeVertex.TC10, ref current, ref n0, ref n1, ref n7, ref n8);
                            vertexBuffer[ind++] = new CubeVertex(ref posNPP, ref CubeVertex.N_POS_Y, ref CubeVertex.TC01, ref current, ref n0, ref n3, ref n4, ref n5);
                            vertexBuffer[ind++] = new CubeVertex(ref posNPP, ref CubeVertex.N_POS_Y, ref CubeVertex.TC01, ref current, ref n0, ref n3, ref n4, ref n5);
                            vertexBuffer[ind++] = new CubeVertex(ref posPPN, ref CubeVertex.N_POS_Y, ref CubeVertex.TC10, ref current, ref n0, ref n1, ref n7, ref n8);
                            vertexBuffer[ind++] = new CubeVertex(ref posPPP, ref CubeVertex.N_POS_Y, ref CubeVertex.TC11, ref current, ref n0, ref n1, ref n2, ref n3);
                        }

                        //-z
                        reg = worldZ - 1;
                        store.GetCube(worldX, y, reg, out n0);

                        if (n0.IsTransparent)
                        {
                            store.GetCube(worldX + 1, y, reg, out n1);
                            store.SafeGetCube(worldX + 1, y + 1, reg, out n2);
                            store.SafeGetCube(worldX, y + 1, reg, out n3);
                            store.SafeGetCube(worldX - 1, y + 1, reg, out n4);
                            store.GetCube(worldX - 1, y, reg, out n5);
                            store.SafeGetCube(worldX - 1, y - 1, reg, out n6);
                            store.SafeGetCube(worldX, y - 1, reg, out n7);
                            store.SafeGetCube(worldX + 1, y - 1, reg, out n8);

                            vertexBuffer[ind++] = new CubeVertex(ref posPPN, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC00, ref current, ref n0, ref n1, ref n2, ref n3);
                            vertexBuffer[ind++] = new CubeVertex(ref posNPN, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC10, ref current, ref n0, ref n3, ref n4, ref n5);
                            vertexBuffer[ind++] = new CubeVertex(ref posPNN, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC01, ref current, ref n0, ref n1, ref n7, ref n8);
                            vertexBuffer[ind++] = new CubeVertex(ref posPNN, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC01, ref current, ref n0, ref n1, ref n7, ref n8);
                            vertexBuffer[ind++] = new CubeVertex(ref posNPN, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC10, ref current, ref n0, ref n3, ref n4, ref n5);
                            vertexBuffer[ind++] = new CubeVertex(ref posNNN, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC11, ref current, ref n0, ref n5, ref n6, ref n7);
                        }

                        //+z
                        reg = worldZ + 1;
                        store.GetCube(worldX, y, reg, out n0);

                        if (n0.IsTransparent)
                        {
                            store.GetCube(worldX + 1, y, reg, out n1);
                            store.SafeGetCube(worldX + 1, y + 1, reg, out n2);
                            store.SafeGetCube(worldX, y + 1, reg, out n3);
                            store.SafeGetCube(worldX - 1, y + 1, reg, out n4);
                            store.GetCube(worldX - 1, y, reg, out n5);
                            store.SafeGetCube(worldX - 1, y - 1, reg, out n6);
                            store.SafeGetCube(worldX, y - 1, reg, out n7);
                            store.SafeGetCube(worldX + 1, y - 1, reg, out n8);

                            vertexBuffer[ind++] = new CubeVertex(ref posNPP, ref CubeVertex.N_POS_Z, ref CubeVertex.TC00, ref current, ref n0, ref n3, ref n4, ref n5);
                            vertexBuffer[ind++] = new CubeVertex(ref posPPP, ref CubeVertex.N_POS_Z, ref CubeVertex.TC10, ref current, ref n0, ref n1, ref n2, ref n3);
                            vertexBuffer[ind++] = new CubeVertex(ref posNNP, ref CubeVertex.N_POS_Z, ref CubeVertex.TC01, ref current, ref n0, ref n5, ref n6, ref n7);
                            vertexBuffer[ind++] = new CubeVertex(ref posNNP, ref CubeVertex.N_POS_Z, ref CubeVertex.TC01, ref current, ref n0, ref n5, ref n6, ref n7);
                            vertexBuffer[ind++] = new CubeVertex(ref posPPP, ref CubeVertex.N_POS_Z, ref CubeVertex.TC10, ref current, ref n0, ref n1, ref n2, ref n3);
                            vertexBuffer[ind++] = new CubeVertex(ref posPNP, ref CubeVertex.N_POS_Z, ref CubeVertex.TC11, ref current, ref n0, ref n1, ref n7, ref n8);
                        }
                    }
                }
            }

            if (ind > 0)
            {
                VertexBuffer = new VertexBuffer(graphics, typeof(CubeVertex), ind, BufferUsage.None);
                VertexBuffer.SetData<CubeVertex>(vertexBuffer,0,ind);
                Empty = false;
            }
            else Empty = true;
        }

        public void CollectSubmeshStats(Chunk parent, CubeStorage store)
        {
            Cube current;
            Cube neighbor;
            int worldX = 0;
            int worldZ = 0;

            int maxY = Chunk.HEIGHT - 1;

            for (int x = 0; x < Chunk.WIDTH; x++)
            {
                worldX = x + parent.Coords.X * Chunk.WIDTH;
                for (int z = 0; z < Chunk.WIDTH; z++)
                {
                    worldZ = z + parent.Coords.Z * Chunk.WIDTH;
                    for (int y = _yStartIndex; y <= _yEndIndex; y++)
                    {
                        store.GetCube(worldX, y, worldZ, out current);
                        if (!current.IsRenderable) continue;

                        RenderableBlocks += 1;

                        //-x
                        store.GetCube(worldX - 1, y, worldZ, out neighbor);
                        if (neighbor.IsTransparent) SidesRenderable++;

                        //+x
                        store.GetCube(worldX + 1, y, worldZ, out neighbor);

                        if (neighbor.IsTransparent) SidesRenderable++;

                        //-y
                        if (y != 0) store.GetCube(worldX, y - 1, worldZ, out neighbor);
                        else neighbor = Cube.NULL;
                        if (neighbor.IsTransparent && neighbor.Type != CubeType.NULL) SidesRenderable++;

                        //+y
                        if (y != maxY) store.GetCube(worldX, y + 1, worldZ, out neighbor);
                        else neighbor = Cube.NULL;
                        if (neighbor.IsTransparent && neighbor.Type != CubeType.NULL) SidesRenderable++;

                        //-z
                        store.GetCube(worldX, y, worldZ - 1, out neighbor);
                        if (neighbor.IsTransparent) SidesRenderable++;

                        //+z
                        store.GetCube(worldX, y, worldZ + 1, out neighbor);
                        if (neighbor.IsTransparent) SidesRenderable++;
                    }
                }
            }
        }

        public void Dispose()
        {
            if(VertexBuffer != null) VertexBuffer.Dispose();
        }
    }
}
