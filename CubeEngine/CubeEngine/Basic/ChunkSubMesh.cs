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
        public const int SIZE_X = Chunk.WIDTH;
        public const int SIZE_Y = Chunk.WIDTH;
        public const int SIZE_Z = Chunk.WIDTH;

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
        public VertexBuffer VertexBuffer;        

        public ChunkSubMesh(int yStartIndex)
        {
            m_yStartIndex = yStartIndex;
            m_yEndIndex = yStartIndex + SIZE_Z - 1;

            Offset = Vector3.Up * yStartIndex;
        }

        public void GetBoundingBox(out BoundingBox boundingBox)
        {
            boundingBox.Max = Position + new Vector3(SIZE_X, SIZE_Y, SIZE_Z);
            boundingBox.Min = Position;            
        }

        public void Update(Vector3 chunkPosition)
        {
            Vector3.Add(ref chunkPosition, ref Offset, out Position);
        }
        public void BuildVertices(List<CubeVertex> vertexList, GraphicsDevice graphics, Cube[,,] parentCubes, Chunk posX, Chunk negX, Chunk posZ, Chunk negZ)
        {
            Cube neighbor;
            Cube current;  
          
            Vector3 offset;
            Vector3 pos1;
            Vector3 pos2;
            Vector3 pos3;
            Vector3 pos4;
            Vector3 pos5;
            Vector3 pos6;
            Vector3 pos7;
            Vector3 pos8;

            int yVal;

            for (byte x = 0; x < SIZE_X; x++)
                for (byte y = 0; y < SIZE_Y; y++)
                    for (byte z = 0; z < SIZE_Z; z++)
                    {
                        yVal = m_yStartIndex + y;

                        current = parentCubes[x, yVal, z];
                        if (!current.IsRenderable()) continue;                        

                        SolidBlocks += 1;

                        offset.X = x;
                        offset.Y = y;
                        offset.Z = z;

                        
                        pos1 = CubeVertex.CORNER_NNN + offset;
                        pos2 = CubeVertex.CORNER_NNP + offset;
                        pos3 = CubeVertex.CORNER_NPN + offset;
                        pos4 = CubeVertex.CORNER_NPP + offset;
                        pos5 = CubeVertex.CORNER_PNN + offset;
                        pos6 = CubeVertex.CORNER_PNP + offset;
                        pos7 = CubeVertex.CORNER_PPN + offset;
                        pos8 = CubeVertex.CORNER_PPP + offset;

                        //-x
                        if (x == 0) neighbor = negX.GetCube(SIZE_X - 1, yVal, z);
                        else neighbor = parentCubes[x - 1, yVal, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new CubeVertex(ref pos3, ref CubeVertex.N_NEG_X, ref CubeVertex.TC00, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos4, ref CubeVertex.N_NEG_X, ref CubeVertex.TC10, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos1, ref CubeVertex.N_NEG_X, ref CubeVertex.TC01, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos1, ref CubeVertex.N_NEG_X, ref CubeVertex.TC01, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos4, ref CubeVertex.N_NEG_X, ref CubeVertex.TC10, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos2, ref CubeVertex.N_NEG_X, ref CubeVertex.TC11, ref current, ref neighbor));                            

                            SidesRenderable += 1;
                        }

                        //+x
                        if (x == SIZE_X - 1) neighbor = posX.GetCube(0, yVal, z);
                        else neighbor = parentCubes[x + 1, yVal, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new CubeVertex(ref pos8, ref CubeVertex.N_POS_X, ref CubeVertex.TC00, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos7, ref CubeVertex.N_POS_X, ref CubeVertex.TC10, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos6, ref CubeVertex.N_POS_X, ref CubeVertex.TC01, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos6, ref CubeVertex.N_POS_X, ref CubeVertex.TC01, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos7, ref CubeVertex.N_POS_X, ref CubeVertex.TC10, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos5, ref CubeVertex.N_POS_X, ref CubeVertex.TC11, ref current, ref neighbor));

                            SidesRenderable += 1;
                        }

                        //-y
                        if (y == 0) neighbor = (m_yStartIndex != 0) ? parentCubes[x, m_yStartIndex - 1, z] : Cube.NULL;
                        else neighbor = parentCubes[x, yVal - 1, z];

                        if (neighbor.IsTransparent() && neighbor.Type != CubeType.NULL)
                        {
                            vertexList.Add(new CubeVertex(ref pos2, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC00, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos6, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC10, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos1, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC01, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos1, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC01, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos6, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC10, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos5, ref CubeVertex.N_NEG_Y, ref CubeVertex.TC11, ref current, ref neighbor));

                            SidesRenderable += 1;
                        }

                        //+y
                        if (y == SIZE_Y - 1) neighbor = (m_yEndIndex != Chunk.HEIGHT - 1) ? parentCubes[x, m_yEndIndex + 1, z] : Cube.NULL;
                        else neighbor = parentCubes[x, yVal + 1, z];

                        if (neighbor.IsTransparent() && neighbor.Type != CubeType.NULL)
                        {
                            vertexList.Add(new CubeVertex(ref pos3, ref CubeVertex.N_POS_Y, ref CubeVertex.TC00, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos7, ref CubeVertex.N_POS_Y, ref CubeVertex.TC10, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos4, ref CubeVertex.N_POS_Y, ref CubeVertex.TC01, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos4, ref CubeVertex.N_POS_Y, ref CubeVertex.TC01, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos7, ref CubeVertex.N_POS_Y, ref CubeVertex.TC10, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos8, ref CubeVertex.N_POS_Y, ref CubeVertex.TC11, ref current, ref neighbor));

                            SidesRenderable += 1;
                        }

                        //-z
                        if (z == 0) neighbor = negZ.GetCube(x, yVal, SIZE_Z - 1);
                        else neighbor = parentCubes[x, yVal, z - 1];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new CubeVertex(ref pos7, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC00, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos3, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC10, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos5, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC01, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos5, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC01, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos3, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC10, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos1, ref CubeVertex.N_NEG_Z, ref CubeVertex.TC11, ref current, ref neighbor));

                            SidesRenderable += 1;
                        }

                        //+z
                        if (z == SIZE_Z - 1) neighbor = posZ.GetCube(x, yVal, 0);
                        else neighbor = parentCubes[x, yVal, z + 1];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new CubeVertex(ref pos4, ref CubeVertex.N_POS_Z, ref CubeVertex.TC00, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos8, ref CubeVertex.N_POS_Z, ref CubeVertex.TC10, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos2, ref CubeVertex.N_POS_Z, ref CubeVertex.TC01, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos2, ref CubeVertex.N_POS_Z, ref CubeVertex.TC01, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos8, ref CubeVertex.N_POS_Z, ref CubeVertex.TC10, ref current, ref neighbor));
                            vertexList.Add(new CubeVertex(ref pos6, ref CubeVertex.N_POS_Z, ref CubeVertex.TC11, ref current, ref neighbor));

                            SidesRenderable += 1;
                        }
                    }

            if (vertexList.Count > 0)
            {
                VertexBuffer = new VertexBuffer(graphics, typeof(CubeVertex), vertexList.Count, BufferUsage.None);
                VertexBuffer.SetData<CubeVertex>(vertexList.ToArray());
                vertexList.Clear();
                Empty = false;
            }
            else Empty = true;

            //if (transparentVertexList.Count > 0)
            //{
            //    TransparentVertexBuffer = new VertexBuffer(graphics, typeof(VertexPositionColor), transparentVertexList.Count, BufferUsage.None);
            //    TransparentVertexBuffer.SetData<VertexPositionColor>(transparentVertexList.ToArray());
            //    transparentVertexList.Clear();
            //}
        }

        public void Dispose()
        {
            if(VertexBuffer != null) VertexBuffer.Dispose();
            //if(TransparentVertexBuffer != null) TransparentVertexBuffer.Dispose();
        }
    }
}
