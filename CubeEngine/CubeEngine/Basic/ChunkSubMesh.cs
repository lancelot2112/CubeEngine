using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CubeEngine.Rendering;



namespace CubeEngine.Basic
{    
    public class ChunkSubMesh
    {
        

        private int m_yStartIndex;
        private int m_yEndIndex;
        public Vector3 Offset;
        public Vector3 Position;

        //Statisitics
        public int SolidBlocks;
        public int SidesRenderable;

        //Render Members
        public bool AllowRender;
        public bool Empty;
        public bool SideCountNeeded;
        public VertexBuffer VertexBuffer;

        public ChunkSubMesh(int yStartIndex, ref Vector3 chunkPosition)
        {
            m_yStartIndex = yStartIndex;
            m_yEndIndex = yStartIndex + Chunk.WIDTH - 1;

            Offset = Vector3.Up * yStartIndex;
            Vector3.Add(ref Offset, ref chunkPosition, out Position);

            SideCountNeeded = true;
        }

        public void GetBoundingBox(out BoundingBox boundingBox)
        {
            boundingBox.Max = Position + new Vector3(Chunk.WIDTH, Chunk.WIDTH, Chunk.WIDTH);
            boundingBox.Min = Position;            
        }

        public void Update(Vector3 chunkPosition)
        {
            Vector3.Add(ref chunkPosition, ref Offset, out Position);
        }
        public void BuildVertices(CubeVertex[] vertexBuffer, GraphicsDevice graphics, Cube[,,] parentCubes, Chunk posX, Chunk negX, Chunk posZ, Chunk negZ)
        {
            if (SideCountNeeded) Initialize(parentCubes, posX, negX, posZ, negZ);
            if (vertexBuffer.Length < SidesRenderable * 6f) vertexBuffer = new CubeVertex[(int)(SidesRenderable * 6.25f)];

            Cube neighbor;
            Cube current;

            int i = 0;
            Vector3 offset;
            Vector3 pos1;
            Vector3 pos2;
            Vector3 pos3;
            Vector3 pos4;
            Vector3 pos5;
            Vector3 pos6;
            Vector3 pos7;
            Vector3 pos8;

            

            int maxIndex = Chunk.WIDTH - 1;
            for (int x = 0; x < Chunk.WIDTH; x++)
                for (int y = m_yStartIndex; y <= m_yEndIndex; y++)
                    for (int z = 0; z < Chunk.WIDTH; z++)
                    {
                        current = parentCubes[x, y, z];
                        if (!current.IsRenderable()) continue;

                        SolidBlocks += 1;

                        offset.X = x;
                        offset.Y = y - m_yStartIndex;
                        offset.Z = z;

                        Vector3.Add(ref CubeVertex.CORNER_NNN, ref offset, out pos1);
                        Vector3.Add(ref CubeVertex.CORNER_NNP, ref offset, out pos2);
                        Vector3.Add(ref CubeVertex.CORNER_NPN, ref offset, out pos3);
                        Vector3.Add(ref CubeVertex.CORNER_NPP, ref offset, out pos4);
                        Vector3.Add(ref CubeVertex.CORNER_PNN, ref offset, out pos5);
                        Vector3.Add(ref CubeVertex.CORNER_PNP, ref offset, out pos6);
                        Vector3.Add(ref CubeVertex.CORNER_PPN, ref offset, out pos7);
                        Vector3.Add(ref CubeVertex.CORNER_PPP, ref offset, out pos8);

                        //-x
                        if (x == 0) neighbor = negX.GetCube(maxIndex, y, z);
                        else neighbor = parentCubes[x - 1, y, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexBuffer[i++] = new CubeVertex(ref pos3, ref CubeVertex.N_NEG_X, ref CubeVertex.TC00, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos4, ref CubeVertex.N_NEG_X, ref CubeVertex.TC10, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos1, ref CubeVertex.N_NEG_X, ref CubeVertex.TC01, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos1, ref CubeVertex.N_NEG_X, ref CubeVertex.TC01, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos4, ref CubeVertex.N_NEG_X, ref CubeVertex.TC10, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos2, ref CubeVertex.N_NEG_X, ref CubeVertex.TC11, ref current, ref neighbor);                       
                        }

                        //+x
                        if (x == maxIndex) neighbor = posX.GetCube(0, y, z);
                        else neighbor = parentCubes[x + 1, y, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexBuffer[i++] = new CubeVertex(ref pos8, ref CubeVertex.N_POS_X, ref CubeVertex.TC00, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos7, ref CubeVertex.N_POS_X, ref CubeVertex.TC10, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos6, ref CubeVertex.N_POS_X, ref CubeVertex.TC01, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos6, ref CubeVertex.N_POS_X, ref CubeVertex.TC01, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos7, ref CubeVertex.N_POS_X, ref CubeVertex.TC10, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos5, ref CubeVertex.N_POS_X, ref CubeVertex.TC11, ref current, ref neighbor);                        
                        }

                        //-y
                        if (y == m_yStartIndex) neighbor = (m_yStartIndex != 0) ? parentCubes[x, y - 1, z] : Cube.NULL;
                        else neighbor = parentCubes[x, y - 1, z];

                        if (neighbor.IsTransparent() && neighbor.Type != CubeType.NULL)
                        {
                            vertexBuffer[i++] = new CubeVertex(ref pos2, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC00, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos6, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC10, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos1, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC01, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos1, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC01, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos6, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC10, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos5, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC11, ref current, ref neighbor);
                        }

                        //+y
                        if (y == m_yEndIndex) neighbor = (m_yEndIndex != Chunk.HEIGHT-1) ? parentCubes[x, y + 1, z] : Cube.NULL;
                        else neighbor = parentCubes[x, y + 1, z];

                        if (neighbor.IsTransparent() && neighbor.Type != CubeType.NULL)
                        {
                            vertexBuffer[i++] = new CubeVertex(ref pos3, ref CubeVertex.N_POS_Y, ref CubeVertex.TC00, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos7, ref CubeVertex.N_POS_Y, ref CubeVertex.TC10, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos4, ref CubeVertex.N_POS_Y, ref CubeVertex.TC01, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos4, ref CubeVertex.N_POS_Y, ref CubeVertex.TC01, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos7, ref CubeVertex.N_POS_Y, ref CubeVertex.TC10, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos8, ref CubeVertex.N_POS_Y, ref CubeVertex.TC11, ref current, ref neighbor);
                        }

                        //-z
                        if (z == 0) neighbor = negZ.GetCube(x, y, maxIndex);
                        else neighbor = parentCubes[x, y, z - 1];

                        if (neighbor.IsTransparent())
                        {
                            vertexBuffer[i++] = new CubeVertex(ref pos7, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC00, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos3, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC10, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos5, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC01, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos5, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC01, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos3, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC10, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos1, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC11, ref current, ref neighbor);                      
                        }

                        //+z
                        if (z == maxIndex) neighbor = posZ.GetCube(x, y, 0);
                        else neighbor = parentCubes[x, y, z + 1];

                        if (neighbor.IsTransparent())
                        {
                            vertexBuffer[i++] = new CubeVertex(ref pos4, ref CubeVertex.N_POS_Z, ref CubeVertex.TC00, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos8, ref CubeVertex.N_POS_Z, ref CubeVertex.TC10, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos2, ref CubeVertex.N_POS_Z, ref CubeVertex.TC01, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos2, ref CubeVertex.N_POS_Z, ref CubeVertex.TC01, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos8, ref CubeVertex.N_POS_Z, ref CubeVertex.TC10, ref current, ref neighbor);
                            vertexBuffer[i++] = new CubeVertex(ref pos6, ref CubeVertex.N_POS_Z, ref CubeVertex.TC11, ref current, ref neighbor);                        
                        }
                    }

            if (i > 0)
            {
                VertexBuffer = new VertexBuffer(graphics, typeof(CubeVertex), vertexBuffer.Length, BufferUsage.None);
                VertexBuffer.SetData<CubeVertex>(vertexBuffer,0,i);
                Empty = false;
            }
            else Empty = true;
        }

        public void Initialize(Cube[, ,] parentCubes, Chunk posX, Chunk negX, Chunk posZ, Chunk negZ)
        {
            SidesRenderable = 0;
            Cube neighbor;
            Cube current;

            int maxIndex = Chunk.WIDTH - 1;
            for (int x = 0; x < Chunk.WIDTH; x++)
                for (int y = m_yStartIndex; y <= m_yEndIndex; y++)
                    for (int z = 0; z < Chunk.WIDTH; z++)
                    {
                        current = parentCubes[x, y, z];
                        if (!current.IsRenderable()) continue;

                        SolidBlocks += 1;

                        //-x
                        if (x == 0) neighbor = negX.GetCube(maxIndex, y, z);
                        else neighbor = parentCubes[x - 1, y, z];

                        if (neighbor.IsTransparent()) SidesRenderable += 1;

                        //+x
                        if (x == maxIndex) neighbor = posX.GetCube(0, y, z);
                        else neighbor = parentCubes[x + 1, y, z];

                        if (neighbor.IsTransparent()) SidesRenderable += 1;

                        //-y
                        if (y == m_yStartIndex) neighbor = (m_yStartIndex != 0) ? parentCubes[x, y - 1, z] : Cube.NULL;
                        else neighbor = parentCubes[x, y - 1, z];

                        if (neighbor.IsTransparent() && neighbor.Type != CubeType.NULL) SidesRenderable += 1;

                        //+y
                        if (y == m_yEndIndex) neighbor = (m_yEndIndex != Chunk.HEIGHT - 1) ? parentCubes[x, y + 1, z] : Cube.NULL;
                        else neighbor = parentCubes[x, y + 1, z];

                        if (neighbor.IsTransparent() && neighbor.Type != CubeType.NULL) SidesRenderable += 1;

                        //-z
                        if (z == 0) neighbor = negZ.GetCube(x, y, maxIndex);
                        else neighbor = parentCubes[x, y, z - 1];

                        if (neighbor.IsTransparent()) SidesRenderable += 1;

                        //+z
                        if (z == maxIndex) neighbor = posZ.GetCube(x, y, 0);
                        else neighbor = parentCubes[x, y, z + 1];

                        if (neighbor.IsTransparent()) SidesRenderable += 1;
                    }

            SideCountNeeded = false;
        }

        public void Dispose()
        {
            if(VertexBuffer != null) VertexBuffer.Dispose();
            //if(TransparentVertexBuffer != null) TransparentVertexBuffer.Dispose();
        }
    }
}
