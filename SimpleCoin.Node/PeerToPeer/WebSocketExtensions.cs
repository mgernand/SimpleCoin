namespace SimpleCoin.Node.PeerToPeer
{
	using System;
	using System.Net.WebSockets;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using Newtonsoft.Json;

	public static class WebSocketExtensions
	{
		public static async Task SendMessage(this WebSocket socket, Message message)
		{
			string data = JsonConvert.SerializeObject(message);

			if (socket.State == WebSocketState.Open)
			{
				ArraySegment<byte> buffer = new ArraySegment<byte>(array: Encoding.UTF8.GetBytes(data), offset: 0, count: data.Length);
				await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
			}
		}
	}
}