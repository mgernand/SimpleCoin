namespace SimpleCoin.Node.PeerToPeer
{
	using System.Net.WebSockets;
	using System.Threading.Tasks;

	public interface IMessageHandler
	{
		Task Handle(WebSocket socket, Message message);
	}
}