using System;

namespace BSPTest
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			using (BSPTest game = new BSPTest())
			{
				game.Run();
			}
		}
	}
}

