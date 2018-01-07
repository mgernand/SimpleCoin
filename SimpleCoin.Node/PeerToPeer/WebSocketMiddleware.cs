namespace SimpleCoin.Node.PeerToPeer
{
	using System;
	using System.Net.WebSockets;
	using System.Threading.Tasks;
	using JetBrains.Annotations;
	using Microsoft.AspNetCore.Http;
	using Microsoft.Extensions.Logging;

	[UsedImplicitly]
	public class WebSocketMiddleware
	{
		private readonly RequestDelegate next;
		private readonly ILogger<WebSocketMiddleware> logger;
		private readonly IWebSocketManager webSocketManager;

		public WebSocketMiddleware(
			RequestDelegate next, 
			ILogger<WebSocketMiddleware> logger,
			IWebSocketManager webSocketManager)
		{
			this.next = next;
			this.logger = logger;
			this.webSocketManager = webSocketManager;
		}

		public async Task Invoke(HttpContext context)
		{
			if (context.WebSockets.IsWebSocketRequest)
			{
				this.logger.LogInformation("Received WebSocket request.");

				WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
				await this.webSocketManager.InitConnection(socket, Guid.NewGuid().ToString("N"));
			}
			else
			{
				await this.next(context);
			}
		}
	}
}