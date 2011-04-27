using System;

namespace ColladaConvert
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			using(ColladaConvert game = new ColladaConvert())
			{
				game.Run();
				Settings.Default.Save();
			}
		}
	}
}