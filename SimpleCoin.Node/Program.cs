namespace SimpleCoin.Node
{
	using System.Linq;
	using Microsoft.AspNetCore;
	using Microsoft.AspNetCore.Hosting;

	public static class Program
	{
		public static void Main(string[] args)
		{
			BuildWebHost(args).Run();
		}

		private static IWebHost BuildWebHost(string[] args)
		{
			string port = args.FirstOrDefault()?.Split("=").LastOrDefault() ?? "5000";
			return WebHost.CreateDefaultBuilder(args)
				.UseUrls($"http://localhost:{port.Trim()}")
				.UseStartup<Startup>()
				.Build();
		}
	}
}
