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
            m_yEndIndex = yStartIndex + SIZE_Z;

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
        public void BuildVertices(List<VertexPositionColor> vertexList, GraphicsDevice graphics, Cube[,,] parentCubes, Chunk posX, Chunk negX, Chunk posZ, Chunk negZ)
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

            CubeVertex vertex;

            Color color;

            for (byte x = 0; x < SIZE_X; x++)
                for (byte y = 0; y < SIZE_Y; y++)
                    for (byte z = 0; z < SIZE_Z; z++)
                    {
                        current = parentCubes[x, y, z];
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

                        color = new Color(current.Red, current.Green, current.Blue); 

                        //-x
                        if (x == 0) neighbor = (negX != null) ? negX.GetCube(SIZE_X - 1, y, z) : Cube.AIR;
                        else neighbor = parentCubes[x - 1, y, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(pos3, color));
                            vertexList.Add(new VertexPositionColor(pos4, color));
                            vertexList.Add(new VertexPositionColor(pos1, color));
                            vertexList.Add(new VertexPositionColor(pos1, color));
                            vertexList.Add(new VertexPositionColor(pos4, color));
                            vertexList.Add(new VertexPositionColor(pos2, color));                            

                            SidesRenderable += 1;
                        }

                        //+x
                        if (x == SIZE_X - 1) neighbor = (posX != null) ? posX.GetCube(0, y, z) : Cube.AIR;
                        else neighbor = parentCubes[x + 1, y, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(pos8, color));
                            vertexList.Add(new VertexPositionColor(pos7, color));
                            vertexList.Add(new VertexPositionColor(pos6, color));
                            vertexList.Add(new VertexPositionColor(pos6, color));
                            vertexList.Add(new VertexPositionColor(pos7, color));
                            vertexList.Add(new VertexPositionColor(pos5, color));

                            SidesRenderable += 1;
                        }

                        //-y
                        if (y == 0) neighbor = (m_yStartIndex != 0) ? parentCubes[x, m_yStartIndex - 1, z] : Cube.AIR;
                        else neighbor = parentCubes[x, y - 1, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(pos2, color));
                            vertexList.Add(new VertexPositionColor(pos6, color));
                            vertexList.Add(new VertexPositionColor(pos1, color));
                            vertexList.Add(new VertexPositionColor(pos1, color));
                            vertexList.Add(new VertexPositionColor(pos6, color));
                            vertexList.Add(new VertexPositionColor(pos5, color));

                            SidesRenderable += 1;
                        }

                        //+y
                        if (y == SIZE_Y - 1) neighbor = (m_yEndIndex != Chunk.HEIGHT) ? parentCubes[x, m_yEndIndex + 1, z] : Cube.AIR;
                        else neighbor = parentCubes[x, y + 1, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(pos3, color));
                            vertexList.Add(new VertexPositionColor(pos7, color));
                            vertexList.Add(new VertexPositionColor(pos4, color));
                            vertexList.Add(new VertexPositionColor(pos4, color));
                            vertexList.Add(new VertexPositionColor(pos7, color));
                            vertexList.Add(new VertexPositionColor(pos8, color));

                            SidesRenderable += 1;
                        }

                        //-z
                        if (z == 0) neighbor = (negZ != null) ? negZ.GetCube(x, y, SIZE_Z - 1) : Cube.AIR;
                        else neighbor = parentCubes[x, y, z - 1];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(pos7, color));
                            vertexList.Add(new VertexPositionColor(pos3, color));
                            vertexList.Add(new VertexPositionColor(pos5, color));
                            vertexList.Add(new VertexPositionColor(pos5, color));
                            vertexList.Add(new VertexPositionColor(pos3, color));
                            vertexList.Add(new VertexPositionColor(pos1, color));

                            SidesRenderable += 1;
                        }

                        //+z
                        if (z == SIZE_Z - 1) neighbor = (posZ != null) ? posZ.GetCube(x, y, 0) : Cube.AIR;
                        else neighbor = parentCubes[x, y, z + 1];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(pos4, color));
                            vertexList.Add(new VertexPositionColor(pos8, color));
                            vertexList.Add(new VertexPositionColor(pos2, color));
                            vertexList.Add(new VertexPositionColor(pos2, color));
                            vertexList.Add(new VertexPositionColor(pos8, color));
                            vertexList.Add(new VertexPositionColor(pos6, color));

                            SidesRenderable += 1;
                        }
                    }

            if (vertexList.Count > 0)
            {
                VertexBuffer = new VertexBuffer(graphics, typeof(VertexPositionColor), vertexList.Count, BufferUsage.None);
                VertexBuffer.SetData<VertexPositionColor>(vertexList.ToArray());
                vertexList.Clear();
                Empty = false;
            }

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
