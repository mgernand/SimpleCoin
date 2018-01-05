namespace SimpleCoin.Node.PeerToPeer
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net.WebSockets;
	using System.Threading.Tasks;
	using Blockchain;
	using JetBrains.Annotations;
	using Microsoft.Extensions.Logging;
	using Newtonsoft.Json;

	[UsedImplicitly]
	public class MessageHandler
	{
		private readonly ILogger<MessageHandler> logger;
		private readonly BlockchainManager blockchainManager;

		public MessageHandler(ILogger<MessageHandler> logger, BlockchainManager blockchainManager)
		{
			this.logger = logger;
			this.blockchainManager = blockchainManager;
		}

		/// <summary>
		/// Handles the given message using a defined message handler method.
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public async Task Handle(WebSocket socket, Message message)
		{
			switch (message.Type)
			{
				case MessageType.Test:
					await this.Test(message.Data);
					break;
				case MessageType.QueryLatest:
					await this.QueryLatest(socket);
					break;
				case MessageType.QueryAll:
					await this.QueryAll(socket);
					break;
				case MessageType.ResponseBlockchain:
					await this.ResponseBlockchain(message.Data);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private Task Test(string data)
		{
			IDictionary<string, object> dataObj = JsonConvert.DeserializeObject<IDictionary<string, object>>(data);

			this.logger.LogInformation($"Received text from peer: {dataObj["text"]}");

			return Task.CompletedTask;

			//await socket.SendMessage(new Message()
			//{
			//	Type = MessageType.Test,
			//	Data = JsonConvert.SerializeObject(data)
			//});
		}

		private Task QueryLatest(WebSocket socket)
		{
			return socket.SendMessage(Message.CreateResponseLatest(this.blockchainManager.Blockchain));
		}

		private Task QueryAll(WebSocket socket)
		{
			return socket.SendMessage(Message.CreateResponseChain(this.blockchainManager.Blockchain));
		}

		private async Task ResponseBlockchain(string data)
		{
			IList<Block> receivedBlocks = JsonConvert.DeserializeObject<IList<Block>>(data);

			if (receivedBlocks == null)
			{
				this.logger.LogError("Received invalid blocks");
				return;
			}

			if (receivedBlocks.Count == 0)
			{
				this.logger.LogError("Received blockchain of size zero");
				return;
			}

			Block latestBlockReceived = receivedBlocks.Last();
			Block latestBlockHeld = this.blockchainManager.Blockchain.GetLatestBlock();

			if (latestBlockReceived.Index > latestBlockHeld.Index)
			{
				this.logger.LogWarning($"Blockchain possibly behind. We have: {latestBlockHeld.Index} the peer has: {latestBlockReceived.Index}");

				if (latestBlockHeld.Hash == latestBlockReceived.Hash)
				{
					if (this.blockchainManager.AddBlock(latestBlockReceived))
					{
						await this.blockchainManager.BroadcastLastest();
					}
				}
				else if (receivedBlocks.Count == 1)
				{
					this.logger.LogInformation("We need to query the chain from the peers");
					await this.blockchainManager.BroadcastQueryAll();
				}
				else
				{
					this.logger.LogInformation("The received blockchain is longer than the current blockchain.");
					this.blockchainManager.ReplaceChain(receivedBlocks);
				}
			}
			else
			{
				this.logger.LogInformation("The received blockchain is not longer than the current blockchain. Do nothing.");
			}
		}
	}
}