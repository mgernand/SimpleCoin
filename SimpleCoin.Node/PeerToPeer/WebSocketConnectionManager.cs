namespace SimpleCoin.Node.PeerToPeer
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net.WebSockets;
	using System.Threading;
	using System.Threading.Tasks;
	using JetBrains.Annotations;

	[UsedImplicitly]
	public class WebSocketConnectionManager : IWebSocketConnectionManager
	{
		private readonly ConcurrentDictionary<string, WebSocket> sockets = new ConcurrentDictionary<string, WebSocket>();

		public WebSocket GetSocketByUrl(string url)
		{
			return this.sockets.FirstOrDefault(p => p.Key == url).Value;
		}

		public void AddSocket(WebSocket socket, string url)
		{
			this.sockets.TryAdd(url, socket);
		}

		public Task RemoveSocket(WebSocket socket)
		{
			string id = this.GetPeerUrl(socket);
			return this.RemoveSocket(id);
		}

		public IList<string> GetPeerUrls()
		{
			return this.sockets.Keys.Where(x => !Guid.TryParse(x, out Guid guid)).ToList();
		}

		public IList<WebSocket> GetPeerWebSockets()
		{
			return this.sockets.Where(x => !Guid.TryParse(x.Key, out Guid guid)).Select(x => x.Value).ToList();
		}

		private async Task RemoveSocket(string url)
		{
			this.sockets.TryRemove(url, out WebSocket socket);

			await socket.CloseAsync(
				closeStatus: WebSocketCloseStatus.NormalClosure,
				statusDescription: "Closed by the WebSocketConnectionManager",
				cancellationToken: CancellationToken.None).ConfigureAwait(false);
		}

		private string GetPeerUrl(WebSocket socket)
		{
			return this.sockets.FirstOrDefault(p => p.Value == socket).Key;
		}
	}
}