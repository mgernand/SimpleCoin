namespace SimpleCoin.Node.Controllers
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Blockchain;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.Extensions.Logging;
	using Newtonsoft.Json;
	using PeerToPeer;
	using Transactions;
	using Wallet;

	/// <summary>
	/// The controller which holds the nodes REST endpoints.
	/// </summary>
	public class NodeController : Controller
	{
		private readonly ILogger<NodeController> logger;
		private readonly WebSocketManager webSocketManager;
		private readonly BlockchainManager blockchainManager;
		private readonly BroadcastService broadcastService;
		private readonly WalletManager walletManager;

		public NodeController(
			ILogger<NodeController> logger, 
			WebSocketManager webSocketManager, 
			BlockchainManager blockchainManager, 
			BroadcastService broadcastService,
			WalletManager walletManager)
		{
			this.logger = logger;
			this.webSocketManager = webSocketManager;
			this.blockchainManager = blockchainManager;
			this.broadcastService = broadcastService;
			this.walletManager = walletManager;
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
		/// <returns></returns>
		[HttpPost("/mineBlock")]
		public IActionResult MineBlock()
		{
			Block newBlock = this.blockchainManager.GenerateNextBlock();

			if (newBlock == null)
			{
				return this.BadRequest("Could not generate block.");
			}

			return this.Ok(newBlock);
		}

		/// <summary>
		/// Add a new block to the chain containing the given raw transaction data.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		[HttpPost("/mineRawBlock")]
		public IActionResult MineRawBlock([FromBody] IDictionary<string, object> data)
		{
			if (!data.ContainsKey("data"))
			{
				return this.BadRequest("Missing data.");
			}

			object transactionData = data["data"];
			string jsonData = JsonConvert.SerializeObject(transactionData);
			IList<Transaction> transactions = JsonConvert.DeserializeObject<IList<Transaction>>(jsonData);
			Block newBlock = this.blockchainManager.GenerateRawNextBlock(transactions);

			if (newBlock == null)
			{
				return this.BadRequest("Could not generate block.");
			}

			return this.Ok(newBlock);
		}

		/// <summary>
		/// Gets the account balance.
		/// </summary>
		/// <returns></returns>

		[HttpGet("/balance")]
		public IActionResult GetBalance()
		{
			long balance = this.blockchainManager.GetAccountBalance();
			return this.Ok(new { balance });
		}

		/// <summary>
		/// Gets the public key (= wallet address).
		/// </summary>
		/// <returns></returns>
		[HttpGet("/address")]
		public IActionResult GetAddress()
		{
			return this.Ok(new { address = this.walletManager.GetPublicKeyFromWallet() });
		}

		/// <summary>
		/// Adds a new block with generated transaction data from receiver account and amount.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		[HttpPost("/mineTransaction")]
		public IActionResult MineTransaction([FromBody] IDictionary<string, object> data)
		{
			if (!data.ContainsKey("address"))
			{
				return this.BadRequest("Missing address.");
			}

			if (!data.ContainsKey("amount"))
			{
				return this.BadRequest("Missing amount.");
			}

			string address = (string) data["address"];
			long amount = (long) data["amount"];

			try
			{
				var response = this.blockchainManager.GenerateNextBlockWithTransaction(address, amount);
				return this.Ok(response);
			}
			catch (InvalidOperationException ex)
			{
				this.logger.LogError(ex, "Error generating block with transaction.");
				return this.BadRequest(ex.Message);
			}
		}
	}
}
