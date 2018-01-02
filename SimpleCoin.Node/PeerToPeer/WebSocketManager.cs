namespace SimpleCoin.Node.PeerToPeer
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net.WebSockets;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using JetBrains.Annotations;
	using Microsoft.Extensions.Logging;
	using Microsoft.Extensions.Options;
	using Newtonsoft.Json;

	[UsedImplicitly]
	public class WebSocketManager
	{
		private readonly ILogger<WebSocketManager> logger;
		private readonly IOptions<ApplicationSettings> appSettings;
		private readonly WebSocketConnectionManager connectionManager;
		private readonly MessageHandler messageHandler;

		public WebSocketManager(ILogger<WebSocketManager> logger, IOptions<ApplicationSettings> appSettings, WebSocketConnectionManager connectionManager, MessageHandler messageHandler)
		{
			this.logger = logger;
			this.appSettings = appSettings;
			this.connectionManager = connectionManager;
			this.messageHandler = messageHandler;
		}

		public async Task InitConnection(WebSocket socket, string url)
		{
			this.connectionManager.AddSocket(socket, url);

			await InitMessageHandler(socket, async (result, message) =>
			{
				if (result.MessageType == WebSocketMessageType.Text)
				{
					await this.messageHandler.Handle(socket, message);
				}
				else if (result.MessageType == WebSocketMessageType.Close)
				{
					await this.connectionManager.RemoveSocket(socket);
				}
			});
		}

		private static async Task InitMessageHandler(WebSocket socket, Action<WebSocketReceiveResult, Message> handleMessage)
		{
			while (socket.State == WebSocketState.Open)
			{
				ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024 * 4]);
				string data = null;
				WebSocketReceiveResult result = null;

				using (MemoryStream ms = new MemoryStream())
				{
					do
					{
						result = await socket.ReceiveAsync(buffer, CancellationToken.None);
						ms.Write(buffer.Array, buffer.Offset, result.Count);
					}
					while (!result.EndOfMessage);

					ms.Seek(0, SeekOrigin.Begin);

					using (StreamReader reader = new StreamReader(ms, Encoding.UTF8))
					{
						data = await reader.ReadToEndAsync();
					}
				}

				Message message = JsonConvert.DeserializeObject<Message>(data);
				handleMessage(result, message);
			}
		}

		public void ConnectToPeer(string url)
		{
			if (!this.IsSelf(url))
			{
				if (!this.IsConnected(url))
				{
					this.logger.LogDebug($"Connecting to peer '{url}'.");

					try
					{
						ClientWebSocket socket = new ClientWebSocket();
						socket.ConnectAsync(new Uri($"ws://{url}/ws"), CancellationToken.None).ContinueWith(task =>
						{
#pragma warning disable 4014
							this.InitConnection(socket, url);
#pragma warning restore 4014
						});
						
					}
					catch (WebSocketException)
					{
						this.logger.LogError($"Connection to peer '{url}' failed.");
					}

					this.logger.LogDebug($"Connection to peer '{url}' successfull.");
				}
				else
				{
					this.logger.LogDebug($"Already connected to peer '{url}'.");
				}
			}
			else
			{
				this.logger.LogDebug("Can not connect to self.");
			}
		}

		private bool IsSelf(string url)
		{
			return url == $"{this.appSettings.Value.Hostname}:{this.appSettings.Value.Port}";
		}

		private bool IsConnected(string url)
		{
			return this.connectionManager.GetPeerUrls().Contains(url);
		}

		public IList<string> GetPeerUrls()
		{
			return this.connectionManager.GetPeerUrls();
		}

		public async Task BroadcastMessage(Message message)
		{
			foreach (WebSocket socket in this.connectionManager.GetPeerWebSockets())
			{
				await socket.SendMessage(message);
			}
		}

		public async Task Shutdown()
		{
			foreach (string url in this.GetPeerUrls())
			{
				WebSocket socket = this.connectionManager.GetSocketByUrl(url);
				await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Node Shutdown", CancellationToken.None);
				await this.connectionManager.RemoveSocket(socket);
			}
		}

		public bool IsAlive(string url)
		{
			WebSocket socket = this.connectionManager.GetSocketByUrl(url);
			return socket.State != WebSocketState.Open || socket.State != WebSocketState.Connecting;
		}

		public async Task Remove(string url)
		{
			WebSocket socket = this.connectionManager.GetSocketByUrl(url);
			await this.connectionManager.RemoveSocket(socket);
		}
	}
}