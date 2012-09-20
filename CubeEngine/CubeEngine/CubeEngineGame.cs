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
        Chunk chunk;
        XerInput input;
        DeltaFreeCamera camera;
        BasicEffect effect;
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
            chunk = new Chunk();
            camera = new DeltaFreeCamera(input, GraphicsDevice);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            chunk.BuildVertices(graphics.GraphicsDevice);

            effect = new BasicEffect(GraphicsDevice);
            effect.VertexColorEnabled = true;

            currentRaster = RasterizerState.CullCounterClockwise;

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            chunk.SolidVertexBuffer.Dispose();
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            camera.Update(dt);

            if(input.Keyboard.F2JustPressed) 
            {
                currentRaster = new RasterizerState();
                currentRaster.CullMode = GraphicsDevice.RasterizerState.FillMode == FillMode.Solid ? CullMode.None : CullMode.CullCounterClockwiseFace;
                currentRaster.FillMode = GraphicsDevice.RasterizerState.FillMode == FillMode.Solid ? FillMode.WireFrame : FillMode.Solid;                
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

            GraphicsDevice.Clear(Color.CornflowerBlue);

            effect.Projection = camera.Projection;
            effect.View = camera.View;

            chunk.WorldMatrix = chunk.WorldMatrix * camera.InverseTranslation;
            effect.World = chunk.WorldMatrix;
            

            GraphicsDevice.SetVertexBuffer(chunk.SolidVertexBuffer);

            effect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, chunk.SolidVertexBuffer.VertexCount / 3);
            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
