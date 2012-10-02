using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using CubeEngine.Basic;

using XerUtilities.Debugging;
using XerUtilities.Input;
using XerUtilities.Rendering;

namespace CubeEngine
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class CubeEngineGame : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        DebugManager debug;
        ChunkManager chunkManager;
        XerInput input;
        DeltaFreeCamera camera;
        Effect effect;
        RasterizerState currentRaster;

        public CubeEngineGame()
        {
            graphics = new GraphicsDeviceManager(this);
            debug = new DebugManager(this, "Font/Arial", true);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            input = new XerInput(this);            
            camera = new DeltaFreeCamera(input, GraphicsDevice);
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            effect = Content.Load<Effect>("Effects/CubeEffect");
            chunkManager = new ChunkManager(GraphicsDevice, new ChunkCoords(0,0,8000), new Vector3(0f,160f,0f), true);
            currentRaster = RasterizerState.CullNone;

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {            
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        ChunkCoords prevPosition = new ChunkCoords();
        float timeTillMove = 0.0f;
        float TimeToMove = 0.5f;
        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            camera.Update(dt);

            timeTillMove += dt;
            if (timeTillMove > TimeToMove)
            {
                timeTillMove -= TimeToMove;
                prevPosition.X += 1;
                prevPosition.Z += 1;
            }

            chunkManager.Update(dt, prevPosition, camera.Translation);

            if(input.Keyboard.F2JustPressed) 
            {
                RasterizerState previous = currentRaster;
                currentRaster = new RasterizerState();
                currentRaster.CullMode = previous.FillMode == FillMode.Solid ? CullMode.None : CullMode.CullCounterClockwiseFace;
                currentRaster.FillMode = previous.FillMode == FillMode.Solid ? FillMode.WireFrame : FillMode.Solid;  
            }

            if (input.Keyboard.F3JustPressed)
            {
                chunkManager.PrintStats();
            }

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.RasterizerState = currentRaster;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            GraphicsDevice.Clear(Color.CornflowerBlue);

            effect.Parameters["Projection"].SetValue(camera.Projection);
            effect.Parameters["View"].SetValue(camera.View);
            effect.Parameters["SkyLightDir"].SetValue(Vector3.Normalize(new Vector3(0.5f,0.75f,1.0f)));

            int VerticesDrawn = 0;
            int SidesDrawn = 0;
            int CubesDrawn = 0;
            int SubMeshesDrawn = 0;
            int TotalSubMeshes = 0;

            Chunk chunk;
            ChunkSubMesh mesh;
            BoundingBox bound;

            for (int i = 0; i < chunkManager.DrawList.Count; i++)
            {
            chunk = chunkManager.DrawList[i];
                for (int j = 0; j < chunk.Meshes.Count; j++)
                {
                    mesh = chunk.Meshes[j];
                    mesh.GetBoundingBox(out bound);
                    BoundingBoxRenderer.Render(bound,GraphicsDevice,camera.View,camera.Projection, Color.Blue);
                    TotalSubMeshes += 1;
                    if (camera.ViewFrustum.Contains(bound) != ContainmentType.Disjoint)
                    {
                        effect.Parameters["World"].SetValue(Matrix.CreateTranslation(mesh.Position));

                        SubMeshesDrawn++;

                        GraphicsDevice.SetVertexBuffer(mesh.VertexBuffer);

                        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, mesh.VertexBuffer.VertexCount / 3);

                            VerticesDrawn += mesh.VertexBuffer.VertexCount;
                            SidesDrawn += mesh.SidesRenderable;
                            CubesDrawn += mesh.SolidBlocks;
                        }
                    }
                }
            }
            


            debug.DebugDisplay.AddLine(1,"vertices: " + (VerticesDrawn*.00001f).ToString() + "*10^6");
            debug.DebugDisplay.AddLine(2,"sides: " + (SidesDrawn*.00001f).ToString() + "*10^6");
            debug.DebugDisplay.AddLine(3,"cube: " + (CubesDrawn*.001f).ToString() + "*10^4");
            debug.DebugDisplay.AddLine(4,"mesh: " + SubMeshesDrawn.ToString() + "/" + TotalSubMeshes.ToString());
            debug.DebugDisplay.AddLine(5, "chunk: " + chunkManager.DrawList.Count.ToString() + "/" + chunkManager.LoadedChunkCount.ToString());
            debug.DebugDisplay.AddLine(6, "pos: " + prevPosition.ToString());
            debug.DebugDisplay.AddLine(7, "updateTotal: " + chunkManager.TotalUpdateTime.ToString() + " ms");
            debug.DebugDisplay.AddLine(8, "queue: " + chunkManager.QueueTime.ToString() + " ms");
            debug.DebugDisplay.AddLine(9, "load: " + chunkManager.LoadTime.ToString() + " ms");
            debug.DebugDisplay.AddLine(10, "light: " + chunkManager.LightTime.ToString() + " ms");
            debug.DebugDisplay.AddLine(11, "build: " + chunkManager.BuildTime.ToString() + " ms");
            debug.DebugDisplay.AddLine(12, "rebuild: " + chunkManager.RebuildTime.ToString() + " ms");
            debug.DebugDisplay.AddLine(13, "update: " + chunkManager.UpdateTime.ToString() + " ms");
            debug.DebugDisplay.AddLine(14, "unload: " + chunkManager.UnloadTime.ToString() + " ms");
            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
