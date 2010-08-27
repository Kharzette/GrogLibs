using System;

namespace BSPBuilder
{
	static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			using (BSPBuilder game = new BSPBuilder())
			{
				game.Run();
				Properties.Settings.Default.Save();
			}
		}
	}
}

