namespace SimpleCoin.Node.PeerToPeer
{
	using System.Collections.Generic;
	using Blockchain;
	using Newtonsoft.Json;

	public class Message
	{
		public MessageType Type { get; set; }

		public string Data { get; set; }

		/// <summary>
		/// Create a message to query the chain length of a peer.
		/// </summary>
		/// <returns></returns>
		public static Message CreateQueryChainLength()
		{
			return new Message
			{
				Type = MessageType.QueryLatest,
				Data = null,
			};
		}

		/// <summary>
		/// Create a message to query ll blocks of a peer.
		/// </summary>
		/// <returns></returns>
		public static Message CreateQueryAll()
		{
			return new Message
			{
				Type = MessageType.QueryAll,
				Data = null,
			};
		}

		/// <summary>
		/// Create a message to send the blockchain in response of a query.
		/// </summary>
		/// <param name="blockchain"></param>
		/// <returns></returns>
		public static Message CreateResponseChain(IList<Block> blockchain)
		{
			return new Message
			{
				Type = MessageType.ResponseBlockchain,
				Data = JsonConvert.SerializeObject(blockchain)
			};
		}

		/// <summary>
		/// Create a message to send the latest block in response of a query.
		/// </summary>
		/// <param name="blockchain"></param>
		/// <returns></returns>
		public static Message CreateResponseLatest(IList<Block> blockchain)
		{
			return new Message
			{
				Type = MessageType.ResponseBlockchain,
				Data = JsonConvert.SerializeObject(new List<Block>{ blockchain.GetLatestBlock() })
			};
		}
	}
}