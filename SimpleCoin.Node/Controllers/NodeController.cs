namespace SimpleCoin.Node.Controllers
{
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Blockchain;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.Extensions.Logging;
	using Newtonsoft.Json;
	using PeerToPeer;
	using Transactions;

	/// <summary>
	/// The controller which holds the nodes REST endpoints.
	/// </summary>
	public class NodeController : Controller
	{
		private readonly ILogger<NodeController> logger;
		private readonly WebSocketManager webSocketManager;
		private readonly BlockchainManager blockchainManager;
		private readonly BroadcastService broadcastService;

		public NodeController(
			ILogger<NodeController> logger, 
			WebSocketManager webSocketManager, 
			BlockchainManager blockchainManager, 
			BroadcastService broadcastService)
		{
			this.logger = logger;
			this.webSocketManager = webSocketManager;
			this.blockchainManager = blockchainManager;
			this.broadcastService = broadcastService;
		}

		/// <summary>
		/// Check if the node is alive.
		/// </summary>
		/// <returns></returns>
		[HttpGet("ping")]
		public IActionResult Ping()
		{
			return this.Ok();
		}

		/// <summary>
		/// Gets the connected peers of the node.
		/// </summary>
		/// <returns></returns>
		[HttpGet("peers")]
		public IActionResult GetPeers()
		{
			IList<string> peers = this.webSocketManager.GetPeerUrls();
			return this.Ok(peers);
		}

		/// <summary>
		/// Adds a new peer to the node.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		[HttpPost("addPeer")]
		public IActionResult AddPeer([FromBody] IDictionary<string, string> data)
		{
			if (!data.ContainsKey("peer"))
			{
				return this.BadRequest("Missing peer url.");
			}

			string peerUrl = data["peer"];
			this.webSocketManager.ConnectToPeer(peerUrl);

			return this.Ok();
		}

		[HttpGet("hello")]
		public async Task<IActionResult> SendToPeers()
		{
			await this.broadcastService.BroadcastMessage(new Message
			{
				Type = MessageType.Test,
				Data = JsonConvert.SerializeObject(new {text = "Hello, World!"})
			});

			return this.Ok();
		}

		/// <summary>
		/// Get all blocks of the blockchain.
		/// </summary>
		/// <returns></returns>
		[HttpGet("/blocks")]
		public IActionResult GetBlockchain()
		{
			return this.Ok(this.blockchainManager.Blockchain);
		}

		/// <summary>
		/// Add a new block to the blockchain.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		[HttpPost("/mineBlock")]
		public IActionResult MineBlock([FromBody] IDictionary<string, object> data)
		{
			if (!data.ContainsKey("data"))
			{
				return this.BadRequest("Missing data.");
			}

			object transactionData = data["data"];
			string jsonData = JsonConvert.SerializeObject(transactionData);
			IList<Transaction> transactions = JsonConvert.DeserializeObject<IList<Transaction>>(jsonData);
			Block newBlock = this.blockchainManager.GenerateNextBlock(transactions);

			if (newBlock == null)
			{
				return this.BadRequest("Could not generate block.");
			}

			return this.Ok(newBlock);
		}


	}
}
