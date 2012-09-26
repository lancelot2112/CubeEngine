using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        //Statisitics
        public int SolidBlocks;
        public int SidesRenderable;

        //Render Members
        public bool AllowRender;
        public bool Empty;
        private List<VertexPositionColor> solidVertexList;
        private List<VertexPositionColor> transparentVertexList;
        public VertexBuffer SolidVertexBuffer;
        public VertexBuffer TransparentVertexBuffer;

        public ChunkSubMesh(int yStartIndex)
        {
            m_yStartIndex = yStartIndex;
            m_yEndIndex = yStartIndex + SIZE_Z;
            solidVertexList = new List<VertexPositionColor>();
            transparentVertexList = new List<VertexPositionColor>();

            Offset = Vector3.Up * yStartIndex;
        }

        public void BuildVertices(GraphicsDevice graphics, Cube[,,] parentCubes, Chunk posX, Chunk negX, Chunk posZ, Chunk negZ)
        {
            Cube neighbor;
            Cube current;
            List<VertexPositionColor> vertexList;
            Vector3 offset;

            for (byte x = 0; x < SIZE_X; x++)
                for (byte y = 0; y < SIZE_Y; y++)
                    for (byte z = 0; z < SIZE_Z; z++)
                    {
                        current = parentCubes[x, y, z];
                        if (!current.IsRenderable()) continue;
                        else if (current.IsTransparent()) vertexList = transparentVertexList;
                        else vertexList = solidVertexList;

                        SolidBlocks += 1;

                        offset.X = x;
                        offset.Y = y;
                        offset.Z = z;

                        //-x
                        if (x == 0) neighbor = (negX != null) ? negX.GetCube(SIZE_X - 1, y, z) : Cube.AIR;
                        else neighbor = parentCubes[x - 1, y, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPN + offset, Color.Green));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPP + offset, Color.Green));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNN + offset, Color.Green));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNN + offset, Color.Green));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPP + offset, Color.Green));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNP + offset, Color.Green));

                            SidesRenderable += 1;
                        }

                        //+x
                        if (x == SIZE_X - 1) neighbor = (posX != null) ? posX.GetCube(0, y, z) : Cube.AIR;
                        else neighbor = parentCubes[x + 1, y, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPP + offset, Color.Blue));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPN + offset, Color.Blue));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNP + offset, Color.Blue));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNP + offset, Color.Blue));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPN + offset, Color.Blue));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNN + offset, Color.Blue));

                            SidesRenderable += 1;
                        }

                        //-y
                        if (y == 0) neighbor = (m_yStartIndex != 0) ? parentCubes[x, m_yStartIndex - 1, z] : Cube.AIR;
                        else neighbor = parentCubes[x, y - 1, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNP + offset, Color.Orange));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNP + offset, Color.Orange));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNN + offset, Color.Orange));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNN + offset, Color.Orange));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNP + offset, Color.Orange));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNN + offset, Color.Orange));

                            SidesRenderable += 1;
                        }

                        //+y
                        if (y == SIZE_Y - 1) neighbor = (m_yEndIndex != Chunk.HEIGHT) ? parentCubes[x, m_yEndIndex + 1, z] : Cube.AIR;
                        else neighbor = parentCubes[x, y + 1, z];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPN + offset, Color.Yellow));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPN + offset, Color.Yellow));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPP + offset, Color.Yellow));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPP + offset, Color.Yellow));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPN + offset, Color.Yellow));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPP + offset, Color.Yellow));

                            SidesRenderable += 1;
                        }

                        //-z
                        if (z == 0) neighbor = (negZ != null) ? negZ.GetCube(x, y, SIZE_Z - 1) : Cube.AIR;
                        else neighbor = parentCubes[x, y, z - 1];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPN + offset, Color.Violet));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPN + offset, Color.Violet));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNN + offset, Color.Violet));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNN + offset, Color.Violet));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPN + offset, Color.Violet));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNN + offset, Color.Violet));

                            SidesRenderable += 1;
                        }

                        //+z
                        if (z == SIZE_Z - 1) neighbor = (posZ != null) ? posZ.GetCube(x, y, 0) : Cube.AIR;
                        else neighbor = parentCubes[x, y, z + 1];

                        if (neighbor.IsTransparent())
                        {
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NPP + offset, Color.Red));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPP + offset, Color.Red));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNP + offset, Color.Red));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_NNP + offset, Color.Red));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PPP + offset, Color.Red));
                            vertexList.Add(new VertexPositionColor(Cube.CORNER_PNP + offset, Color.Red));

                            SidesRenderable += 1;
                        }
                    }

            if (solidVertexList.Count > 0)
            {
                SolidVertexBuffer = new VertexBuffer(graphics, typeof(VertexPositionColor), solidVertexList.Count, BufferUsage.None);
                SolidVertexBuffer.SetData<VertexPositionColor>(solidVertexList.ToArray());
                solidVertexList.Clear();
                Empty = false;
            }

            if (transparentVertexList.Count > 0)
            {
                TransparentVertexBuffer = new VertexBuffer(graphics, typeof(VertexPositionColor), transparentVertexList.Count, BufferUsage.None);
                TransparentVertexBuffer.SetData<VertexPositionColor>(transparentVertexList.ToArray());
                transparentVertexList.Clear();
            }
        }

        public void Dispose()
        {
            if(SolidVertexBuffer != null) SolidVertexBuffer.Dispose();
            if(TransparentVertexBuffer != null) TransparentVertexBuffer.Dispose();
        }
    }
}
