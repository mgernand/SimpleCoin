namespace SimpleCoin.Node.PeerToPeer
{
	using System.Collections.Generic;
	using System.Net.WebSockets;
	using System.Threading.Tasks;

	public interface IWebSocketConnectionManager
	{
		void AddSocket(WebSocket socket, string url);

		IList<string> GetPeerUrls();

		IList<WebSocket> GetPeerWebSockets();

		WebSocket GetSocketByUrl(string url);

		Task RemoveSocket(WebSocket socket);
	}
}