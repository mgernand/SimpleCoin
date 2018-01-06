namespace SimpleCoin.Node.PeerToPeer
{
	using System.Collections.Generic;
	using System.Net.WebSockets;
	using System.Threading.Tasks;
	using Blockchain;
	using JetBrains.Annotations;
	using Microsoft.Extensions.Logging;
	using Transactions;

	[UsedImplicitly]
	public class BroadcastService
	{
		private readonly ILogger<BroadcastService> logger;
		private readonly WebSocketConnectionManager connectionManager;

		public BroadcastService(ILogger<BroadcastService> logger, WebSocketConnectionManager connectionManager)
		{
			this.logger = logger;
			this.connectionManager = connectionManager;
		}

		/// <summary>
		/// Broadcast a message to the connected peers.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public async Task BroadcastMessage(Message message)
		{
			foreach (WebSocket socket in this.connectionManager.GetPeerWebSockets())
			{
				await socket.SendMessage(message);
			}
		}

		/// <summary>
		/// Broadcast the latest block to the connected peers.
		/// </summary>
		/// <param name="blockchain"></param>
		/// <returns></returns>
		public Task BroadcastLastest(IList<Block> blockchain)
		{
			return this.BroadcastMessage(Message.CreateResponseLatest(blockchain));
		}

		/// <summary>
		/// Boardcast the query for all blocks of the peer to the connected peers.
		/// </summary>
		/// <returns></returns>
		public Task BroadcastQueryAll()
		{
			return this.BroadcastMessage(Message.CreateQueryAll());
		}

		public Task BroadcastTransactionPool(IList<Transaction> transactionPool)
		{
			return this.BroadcastMessage(Message.CreateResponseTransactionPool(transactionPool));
		}
	}
}