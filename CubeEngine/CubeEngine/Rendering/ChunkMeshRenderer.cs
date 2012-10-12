using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

using XerUtilities.Rendering;

using CubeEngine.Basic;





namespace CubeEngine.Rendering
{
    public class ChunkMeshRenderer
    {
        private List<ChunkMesh> _solidMeshes;
        private List<ChunkMesh> _transparentMeshes;

        private Queue<ChunkMesh> _solidMeshRenderQueue;
        private Queue<ChunkMesh> _transparentMeshRenderQueue;

        private ChunkCoordinate _previous;

        private Effect _solidEffect;

        public ChunkMeshRenderer(Game game)
        {
            _solidMeshes = new List<ChunkMesh>();
            _transparentMeshes = new List<ChunkMesh>();

            _solidMeshRenderQueue = new Queue<ChunkMesh>();
            _transparentMeshRenderQueue = new Queue<ChunkMesh>();

            _solidEffect = game.Content.Load<Effect>("Effects/CubeEffect");
        }

        public void AddChunkMeshes(Chunk chunk)
        {
            for (int i = 0; i < chunk.Meshes.Count; i++)
            {
                AddMesh(chunk.Meshes[i]);
            }
        }

        public void RemoveChunkMeshes(Chunk chunk)
        {
            for (int i = 0; i < chunk.Meshes.Count; i++)
            {
                RemoveMesh(chunk.Meshes[i]);
            }
        }

        public void AddMesh(ChunkMesh mesh)
        {
            if (mesh.Transparent) _transparentMeshes.Add(mesh);
            else _solidMeshes.Add(mesh);
        }

        public void RemoveMesh(ChunkMesh mesh)
        {
            if (mesh.Transparent) _transparentMeshes.Remove(mesh);
            else _solidMeshes.Remove(mesh);
        }

        public void CullMeshes(ref ChunkCoordinate current, ref BoundingFrustum frustum)
        {
            BoundingBox box;            
            if (!current.Equals(ref _previous))
            {
                _solidMeshes.Sort((x, y) => x.Position.LengthSquared().CompareTo(y.Position.LengthSquared()));
                _transparentMeshes.Sort((x, y) => -x.Position.LengthSquared().CompareTo(y.Position.LengthSquared()));
            }

            for (int i = 0; i < _solidMeshes.Count; i++)
            {
                _solidMeshes[i].GetBoundingBox(out box);
                if (frustum.Contains(box) != ContainmentType.Disjoint)
                {
                    _solidMeshRenderQueue.Enqueue(_solidMeshes[i]);
                }
            }

            for (int i = 0; i < _transparentMeshes.Count; i++)
            {
               _transparentMeshes[i].GetBoundingBox(out box);
               if (frustum.Contains(box) != ContainmentType.Disjoint)
               {
                   _transparentMeshRenderQueue.Enqueue(_transparentMeshes[i]);
               }
            }
        }

        public void Draw(GraphicsDevice graphics, Camera camera)
        {
            ChunkMesh mesh;

            _solidEffect.Parameters["Projection"].SetValue(camera.Projection);
            _solidEffect.Parameters["View"].SetValue(camera.View);
            _solidEffect.Parameters["SkyLightDir"].SetValue(Vector3.Normalize(new Vector3(0.5f, 0.75f, 1.0f)));
            _solidEffect.Parameters["FogColor"].SetValue(Color.CornflowerBlue.ToVector4());

            while (_solidMeshRenderQueue.Count > 0)
            {
                mesh = _solidMeshRenderQueue.Dequeue();

                _solidEffect.Parameters["World"].SetValue(Matrix.CreateTranslation(mesh.Position));

                graphics.SetVertexBuffer(mesh.VertexBuffer);

                foreach (EffectPass pass in _solidEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphics.DrawPrimitives(PrimitiveType.TriangleList, 0, mesh.VertexBuffer.VertexCount / 3);
                }
            }
        }
    }
}
