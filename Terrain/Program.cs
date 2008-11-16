using System;

namespace Terrain
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			using(Terrain game = new Terrain(args))
			{
				game.Run();
			}
		}
	}
}

