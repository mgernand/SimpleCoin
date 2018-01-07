namespace SimpleCoin.Node
{
	using System;
	using Blockchain;
	using JetBrains.Annotations;
	using Microsoft.AspNetCore.Builder;
	using Microsoft.AspNetCore.Hosting;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Hosting;
	using Microsoft.Extensions.Logging;
	using PeerToPeer;
	using Transactions;
	using Wallet;

	[UsedImplicitly]
	public sealed class Startup
	{
		public Startup(IConfiguration configuration)
		{
			this.Configuration = configuration;
		}

		private IConfiguration Configuration { get; }

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

			services.AddTransient<IWebSocketManager, WebSocketManager>();
			services.AddSingleton<IWebSocketConnectionManager, WebSocketConnectionManager>();
			services.AddTransient<IMessageHandler, MessageHandler>();
			services.AddTransient<IBroadcastService, BroadcastService>();
			services.AddSingleton<IHostedService, PeerDiscoveryService>();
			services.AddSingleton<IBlockchainManager, BlockchainManager>();
			services.AddTransient<ITransactionManager, TransactionManager>();
			services.AddSingleton<ITransactionPoolManager, TransactionPoolManager>();
			services.AddTransient<IWalletManager, WalletManager>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
		{
			IWalletManager walletManager = serviceProvider.GetService<IWalletManager>();
			walletManager.InitWallet();

			app.UseWebSockets();
			app.UseMvc();

			app.Map("/ws", x => x.UseMiddleware<WebSocketMiddleware>(
				serviceProvider.GetService<ILogger<WebSocketMiddleware>>(), 
				serviceProvider.GetService<IWebSocketManager>()));
		}
	}
}
