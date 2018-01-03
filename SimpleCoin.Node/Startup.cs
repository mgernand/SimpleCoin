namespace SimpleCoin.Node
{
	using System;
	using Blockchain;
	using Microsoft.AspNetCore.Builder;
	using Microsoft.AspNetCore.Hosting;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Hosting;
	using Microsoft.Extensions.Logging;
	using PeerToPeer;

	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			this.Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMvc()
				.AddControllersAsServices();

			services.AddOptions();
			services.Configure<ApplicationSettings>(options =>
			{
				Uri selfUri = new Uri(this.Configuration["URLS"]);

				options.Hostname = selfUri.Host;
				options.Port = (ushort) selfUri.Port;
			});

			services.AddTransient<WebSocketManager>();
			services.AddSingleton<WebSocketConnectionManager>();
			services.AddTransient<MessageHandler>();
			services.AddSingleton<IHostedService, PeerDiscoveryService>();
			services.AddTransient<BroadcastService>();
			services.AddSingleton<BlockchainManager>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseWebSockets();
			app.UseMvc();

			app.Map("/ws", x => x.UseMiddleware<WebSocketMiddleware>(
				serviceProvider.GetService<ILogger<WebSocketMiddleware>>(), 
				serviceProvider.GetService<WebSocketManager>()));
		}
	}
}
