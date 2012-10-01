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
        public VertexBuffer VertexBuffer;

        public ChunkSubMesh(int yStartIndex)
        {
            m_yStartIndex = yStartIndex;
            m_yEndIndex = yStartIndex + Chunk.WIDTH - 1;

            Offset = Vector3.Up * yStartIndex;            
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
                        
                        pos1 = CubeVertex.CORNER_NNN + offset;
                        pos2 = CubeVertex.CORNER_NNP + offset;
                        pos3 = CubeVertex.CORNER_NPN + offset;
                        pos4 = CubeVertex.CORNER_NPP + offset;
                        pos5 = CubeVertex.CORNER_PNN + offset;
                        pos6 = CubeVertex.CORNER_PNP + offset;
                        pos7 = CubeVertex.CORNER_PPN + offset;
                        pos8 = CubeVertex.CORNER_PPP + offset;

                        //-x
                        if (x == 0) neighbor = negX.GetCube(maxIndex, y, z);
                        else neighbor = parentCubes[x - 1, y, z];

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
                        if (x == maxIndex) neighbor = posX.GetCube(0, y, z);
                        else neighbor = parentCubes[x + 1, y, z];

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
                        if (y == m_yStartIndex) neighbor = (m_yStartIndex != 0) ? parentCubes[x, y - 1, z] : Cube.NULL;
                        else neighbor = parentCubes[x, y - 1, z];

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
                        if (y == m_yEndIndex) neighbor = (m_yEndIndex != Chunk.HEIGHT-1) ? parentCubes[x, y + 1, z] : Cube.NULL;
                        else neighbor = parentCubes[x, y + 1, z];

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
                        if (z == 0) neighbor = negZ.GetCube(x, y, maxIndex);
                        else neighbor = parentCubes[x, y, z - 1];

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
                        if (z == maxIndex) neighbor = posZ.GetCube(x, y, 0);
                        else neighbor = parentCubes[x, y, z + 1];

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
