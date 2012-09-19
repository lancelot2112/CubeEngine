using System;

namespace CubeEngine
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (CubeEngineGame game = new CubeEngineGame())
            {
                game.Run();
            }
        }
    }
#endif
}

