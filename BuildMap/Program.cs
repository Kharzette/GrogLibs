using System;

namespace BuildMap
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (BuildMapMain game = new BuildMapMain(args))
            {
                game.Run();
            }
        }
    }
}

