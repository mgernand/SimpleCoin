namespace SimpleCoin.Node.PeerToPeer
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net.Http;
	using System.Threading;
	using System.Threading.Tasks;
	using JetBrains.Annotations;
	using Microsoft.Extensions.Logging;
	using Microsoft.Extensions.Options;
	using Newtonsoft.Json;

	[UsedImplicitly]
	public class PeerDiscoveryService : HostedService
	{
		private static readonly HttpClient client = new HttpClient();

		private readonly ILogger<PeerDiscoveryService> logger;
		private readonly IOptions<ApplicationSettings> appSettings;
		private readonly IWebSocketManager webSocketManager;

		private readonly IList<string> seedNodeUrls = new List<string>
		{
			"localhost:5000"
		};

		public PeerDiscoveryService(
			ILogger<PeerDiscoveryService> logger, 
			IOptions<ApplicationSettings> appSettings, 
			IWebSocketManager webSocketManager)
		{
			this.logger = logger;
			this.appSettings = appSettings;
			this.webSocketManager = webSocketManager;
		}

		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					List<string> nodes = new List<string>(this.seedNodeUrls);
					IList<string> peers = this.webSocketManager.GetPeerUrls();

					if (peers.Count > 0)
					{
						this.logger.LogDebug($"Updating peer nodes. Known peers: {peers.Aggregate((s1, s2) => string.Concat(s1, ", ", s2))}");
					}

					nodes.AddRange(peers);

					int count = nodes.Count;
					if (this.IsSeedNode)
					{
						count -= this.seedNodeUrls.Count;
					}

					this.logger.LogDebug($"Asking {count} nodes for known peers.");

					// Check for dead socket connections.
					foreach (string peer in peers)
					{
						if (!this.webSocketManager.IsAlive(peer) || !await IsNodeAlive(peer))
						{
							await this.webSocketManager.Remove(peer);
						}
					}

					// Keep connections to the seed nodes.
					foreach (string seedNode in this.seedNodeUrls)
					{
						// Do not connect or add us to ourself.
						if (!this.IsSeedNode)
						{
							this.webSocketManager.ConnectToPeer(seedNode);

							// Add the node to the seed nodes.
							await client.PostAsync($"http://{seedNode}/addPeer",
								new JsonContent(new {peer = $"{this.appSettings.Value.Hostname}:{this.appSettings.Value.Port}"}),
								cancellationToken);
						}
					}

					// Asking kown nodes for their known nodes.
					foreach (string nodeUrl in nodes)
					{
						// Ignore asking the seed node when we are a seed node.
						if (!this.IsSeedNode)
						{
							this.logger.LogDebug($"Getting peers from node '{nodeUrl}'.");

							HttpResponseMessage response = await client.GetAsync($"http://{nodeUrl}/peers", cancellationToken);
							string jsonData = await response.Content.ReadAsStringAsync();
							string[] knownNodes = JsonConvert.DeserializeObject<string[]>(jsonData);

							this.logger.LogDebug(knownNodes.Length > 0
								? $"Known nodes of '{nodeUrl}': {knownNodes.Aggregate((s1, s2) => string.Concat(s1, ", ", s2))}"
								: $"No known nodes of '{nodeUrl}'.");

							foreach (string knownNode in knownNodes)
							{
								this.webSocketManager.ConnectToPeer(knownNode);
							}
						}
					}

					await Task.Delay(TimeSpan.FromMilliseconds(5000), cancellationToken);
				}
				catch (HttpRequestException ex)
				{
					this.logger.LogError(ex, "Error getting peers.");
				}
				catch (Exception ex)
				{
					if (ex is OperationCanceledException)
					{
						throw;
					}

					this.logger.LogError(ex, "Unhandled Error");
				}
			}

			// The node was cancelled, close all open connections to peers.
			this.logger.LogDebug("Shutting down websockets.");
			await this.webSocketManager.Shutdown();
		}

		private static async Task<bool> IsNodeAlive(string nodeUrl)
		{
			try
			{
				HttpResponseMessage response = await client.GetAsync($"http://{nodeUrl}/ping");
				return response.IsSuccessStatusCode;
			}
			catch
			{
				return false;
			}
		}

		private bool IsSeedNode => this.seedNodeUrls.Contains($"{this.appSettings.Value.Hostname}:{this.appSettings.Value.Port}");
	}
}