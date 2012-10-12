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
using System.Diagnostics;
using CubeEngine.Rendering;

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
        RasterizerState currentRaster;
        Stopwatch watch;

        public CubeEngineGame()
        {
            graphics = new GraphicsDeviceManager(this);
            debug = new DebugManager(this, "Font/Arial", true);
            debug.Console.Execute("clr.AddReference(\"CubeEngine\")");
            debug.Console.Execute("from CubeEngine import *");
            debug.Console.AddObject("game", this);
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
            debug.Console.AddObject("camera", camera);
            watch = new Stopwatch();
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            chunkManager = new ChunkManager(this, new ChunkCoordinate(0,0,8000), new Vector3(0f,Chunk.HEIGHT * 0.9f,0f), true);
            debug.Console.AddObject("chunkManager", chunkManager);
            currentRaster = RasterizerState.CullCounterClockwise;

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
        ChunkCoordinate prevPosition = new ChunkCoordinate();
        Vector2 move = Vector2.Zero;
        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            camera.Update(dt);

            move.X += camera.Translation.X;
            move.Y += camera.Translation.Z;

            prevPosition.X = (int)(move.X / Chunk.WIDTH);
            prevPosition.Z = (int)(move.Y / Chunk.WIDTH);

            chunkManager.Update(dt, prevPosition, camera.Translation);

            if (input.Keyboard.F3JustPressed)
            {
                chunkManager.PrintStats();
            }

            if (input.Keyboard.F4JustPressed)
            {
                chunkManager.Reload();
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
            watch.Reset();
            watch.Start();
            GraphicsDevice.RasterizerState = currentRaster;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            GraphicsDevice.Clear(Color.CornflowerBlue);

            chunkManager.Draw(GraphicsDevice, camera);

            watch.Stop();

            debug.DebugDisplay.AddLine(6, "pos: " + prevPosition.ToString());
            debug.DebugDisplay.AddLine(7, "posC: " + chunkManager.PlayerPosition.ToString());
            debug.DebugDisplay.AddLine(8, "updateTotal: " + chunkManager.TotalUpdateTime.ToString() + " ms");
            debug.DebugDisplay.AddLine(9, "queue: " + chunkManager.QueueTime.ToString() + " ms");
            debug.DebugDisplay.AddLine(10, "load: " + chunkManager.LoadTime.ToString() + " ms");
            debug.DebugDisplay.AddLine(11, "light: " + chunkManager.LightTime.ToString() + " ms");
            debug.DebugDisplay.AddLine(12, "build: " + chunkManager.BuildTime.ToString() + " ms");
            debug.DebugDisplay.AddLine(13, "rebuild: " + chunkManager.RebuildTime.ToString() + " ms");
            debug.DebugDisplay.AddLine(14, "update: " + chunkManager.UpdateTime.ToString() + " ms");
            debug.DebugDisplay.AddLine(15, "unload: " + chunkManager.UnloadTime.ToString() + " ms");
            debug.DebugDisplay.AddLine(16, "draw: " + watch.Elapsed.TotalMilliseconds.ToString() + " ms");
            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
