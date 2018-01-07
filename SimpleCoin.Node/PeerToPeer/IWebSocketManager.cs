namespace SimpleCoin.Node.PeerToPeer
{
	using System.Collections.Generic;
	using System.Net.WebSockets;
	using System.Threading.Tasks;

	public interface IWebSocketManager
	{
		void ConnectToPeer(string url);

		IList<string> GetPeerUrls();

		Task InitConnection(WebSocket socket, string url);

		bool IsAlive(string url);

		Task Remove(string url);

		Task Shutdown();
	}
}