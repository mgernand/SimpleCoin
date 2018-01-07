namespace SimpleCoin.Node.Controllers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Blockchain;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.Extensions.Logging;
	using Newtonsoft.Json;
	using PeerToPeer;
	using Transactions;
	using Wallet;

	/// <summary>
	/// The controller with the nodes REST endpoints.
	/// </summary>
	public class NodeController : Controller
	{
		private readonly ILogger<NodeController> logger;
		private readonly IWebSocketManager webSocketManager;
		private readonly IBlockchainManager blockchainManager;
		private readonly IBroadcastService broadcastService;
		private readonly IWalletManager walletManager;
		private readonly ITransactionPoolManager transactionPoolManager;

		public NodeController(
			ILogger<NodeController> logger,
			IWebSocketManager webSocketManager,
			IBlockchainManager blockchainManager,
			IBroadcastService broadcastService,
			IWalletManager walletManager,
			ITransactionPoolManager transactionPoolManager)
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
		/// Send a test broadcast to all connected peers.
		/// </summary>
		/// <returns></returns>
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
		[HttpPost("peers")]
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
		[HttpGet("blocks/{hash}")]
		public IActionResult GetBlock(string hash)
		{
			Block block = this.blockchainManager.Blockchain.FirstOrDefault(x => x.Hash == hash);

			if (block == null)
			{
				return this.NotFound(GetErrorResult($"Block with hash {hash} not found."));
			}

			return this.Ok(block);
		}

		/// <summary>
		/// Gets a specific transaction.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[HttpGet("/transaction/{id}")]
		[HttpGet("/transactions/{id}")]
		public IActionResult GetTransaction(string id)
		{
			Transaction transaction = this.blockchainManager.Blockchain
				.SelectMany(x => x.Data)
				.FirstOrDefault(x => x.Id == id);

			if (transaction == null)
			{
				return this.NotFound(GetErrorResult($"Transaction with id {id} not found."));
			}

			return this.Ok(transaction);
		}

		/// <summary>
		/// Gets the unspent transaction outputs of the given address.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		[HttpGet("/address/{address}")]
		[HttpGet("/uTxOuts/{address}")]
		[HttpGet("/unspentTxOuts/{address}")]
		[HttpGet("/unspentTransactionOutputs/{address}")]
		public IActionResult GetUnspentTxOuts(string address)
		{
			return this.Ok(this.blockchainManager.GetUnspentTxOuts(address));
		}

		/// <summary>
		/// Get the unspent transaction outputs.
		/// </summary>
		/// <returns></returns>
		[HttpGet("/uTxOuts")]
		[HttpGet("/unspentTxOuts")]
		[HttpGet("/unspentTransactionOutputs")]
		public IActionResult GetUnspentTxOuts()
		{
			return this.Ok(this.blockchainManager.UnspentTxOuts);
		}

		/// <summary>
		/// Gets the unspent transaction outputs owned by the wallet.
		/// </summary>
		/// <returns></returns>
		[HttpGet("/myUTxOuts")]
		[HttpGet("/myUnspentTxOuts")]
		[HttpGet("/myUnspentTransactionOutputs")]
		public IActionResult GetMyUnspentTxOuts()
		{
			return this.Ok(this.blockchainManager.GetUnspentTxOuts(this.walletManager.GetPublicKeyFromWallet()));
		}

		/// <summary>
		/// Gets the account balance of the wallet.
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
		/// Gets the public key (= wallet address) of the wallet.
		/// </summary>
		/// <returns></returns>
		[HttpGet("/address")]
		public IActionResult GetAddress()
		{
			return this.Ok(new { address = this.walletManager.GetPublicKeyFromWallet() });
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
		/// Sends a transaction to the network to be processed.
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
		/// Gets the unconfirmed transactions.
		/// </summary>
		/// <returns></returns>
		[HttpGet("/transactionPool")]
		[HttpGet("/transaction_pool")]
		public IActionResult GetTransactionPool()
		{
			return this.Ok(this.transactionPoolManager.TransactionPool);
		}

		private static object GetErrorResult(string message)
		{
			return new { message };
		}
	}
}
