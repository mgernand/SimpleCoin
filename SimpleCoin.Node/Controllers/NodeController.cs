namespace SimpleCoin.Node.Controllers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection.Metadata.Ecma335;
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
		private readonly TransactionPoolManager transactionPoolManager;

		public NodeController(
			ILogger<NodeController> logger, 
			WebSocketManager webSocketManager, 
			BlockchainManager blockchainManager, 
			BroadcastService broadcastService,
			WalletManager walletManager,
			TransactionPoolManager transactionPoolManager)
		{
			this.logger = logger;
			this.webSocketManager = webSocketManager;
			this.blockchainManager = blockchainManager;
			this.broadcastService = broadcastService;
			this.walletManager = walletManager;
			this.transactionPoolManager = transactionPoolManager;
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
				return this.BadRequest(GetErrorResult("Missing peer url."));
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
				Data = JsonConvert.SerializeObject(new { text = "Hello, World!" })
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
		/// Gets a specific block.
		/// </summary>
		/// <param name="hash"></param>
		/// <returns></returns>
		[HttpGet("block/{hash}")]
		public IActionResult GetBlock(string hash)
		{
			Block block = this.blockchainManager.Blockchain.FirstOrDefault(x => x.Hash == hash);

			if (block == null)
			{
				return this.NotFound(hash);
			}

			return this.Ok(block);
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
				return this.BadRequest(GetErrorResult("Could not generate block."));
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
				return this.BadRequest(GetErrorResult("Missing data."));
			}

			object transactionData = data["data"];
			string jsonData = JsonConvert.SerializeObject(transactionData);
			IList<Transaction> transactions = JsonConvert.DeserializeObject<IList<Transaction>>(jsonData);
			Block newBlock = this.blockchainManager.GenerateRawNextBlock(transactions);

			if (newBlock == null)
			{
				return this.BadRequest(GetErrorResult("Could not generate block."));
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
		/// Gets the balance of a specific address.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		[HttpGet("balance/{address}")]
		public IActionResult GetBalance(string address)
		{
			long balance = this.blockchainManager.GetAccountBalance(address);
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
				return this.BadRequest(GetErrorResult("Missing address."));
			}

			if (!data.ContainsKey("amount"))
			{
				return this.BadRequest(GetErrorResult("Missing amount."));
			}

			string address = (string) data["address"];
			long amount = (long) data["amount"];

			try
			{
				Block newBlock = this.blockchainManager.GenerateNextBlockWithTransaction(address, amount);
				return this.Ok(newBlock);
			}
			catch (InvalidOperationException ex)
			{
				this.logger.LogCritical(ex, "Error generating block with transaction.");
				return this.BadRequest(GetErrorResult(ex.Message));
			}
		}

		/// <summary>
		/// Sends a transaction to the transaction pool to be processed.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		[HttpPost("sendTransaction")]
		public IActionResult SendTransaction([FromBody] IDictionary<string, object> data)
		{
			if (!data.ContainsKey("address"))
			{
				return this.BadRequest(GetErrorResult("Missing address."));
			}

			if (!data.ContainsKey("amount"))
			{
				return this.BadRequest(GetErrorResult("Missing amount."));
			}

			string address = (string)data["address"];
			long amount = (long)data["amount"];

			try
			{
				Transaction transaction = this.blockchainManager.SendTransaction(address, amount);
				return this.Ok(transaction);
			}
			catch (InvalidOperationException ex)
			{
				this.logger.LogCritical(ex, "Error generating block with transaction.");
				return this.BadRequest(GetErrorResult(ex.Message));
			}
		}

		/// <summary>
		/// Gets the transaction pool.
		/// </summary>
		/// <returns></returns>
		[HttpGet("/transactionPool")]
		public IActionResult GetTransactionPool()
		{
			return this.Ok(this.transactionPoolManager.TransactionPool);
		}

		/// <summary>
		/// Gets a specific transaction.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[HttpGet("/transactions/{id}")]
		public IActionResult GetTransaction(string id)
		{
			Transaction transaction = this.blockchainManager.Blockchain
				.SelectMany(x => x.Data)
				.FirstOrDefault(x => x.Id == id);

			if (transaction == null)
			{
				return this.NotFound(id);
			}

			return this.Ok(transaction);
		}

		/// <summary>
		/// Gets the unspent TxOuts of the given address.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		[HttpGet("/unspentTxOuts/{address}")]
		public IActionResult GetUnspentTxOuts(string address)
		{
			return this.Ok(this.blockchainManager.GetUnspentTxOuts(address));
		}

		/// <summary>
		/// Get the unspent TxOuts.
		/// </summary>
		/// <returns></returns>
		[HttpGet("/unspentTxOuts")]
		public IActionResult GetUnspentTxOuts()
		{
			return this.Ok(this.blockchainManager.UnspentTxOuts);
		}

		private static object GetErrorResult(string message)
		{
			return new { message };
		}
	}
}
