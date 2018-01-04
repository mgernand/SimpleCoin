namespace SimpleCoin.Node.Blockchain
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Logging;
	using Newtonsoft.Json;
	using PeerToPeer;
	using Transactions;
	using Util;

	[UsedImplicitly]
	public class BlockchainManager
	{
		private readonly ILogger<BlockchainManager> logger;
		private readonly BroadcastService broadcastService;
		private readonly TransactionManager transactionManager;

		public BlockchainManager(ILogger<BlockchainManager> logger, BroadcastService broadcastService, TransactionManager transactionManager)
		{
			this.logger = logger;
			this.broadcastService = broadcastService;
			this.transactionManager = transactionManager;

			this.Blockchain = new List<Block> { Block.Genesis };
			this.UnspentTxOuts = new List<UnspentTxOut>();
		}


		/// <summary>
		/// In-memory stored blockchain.
		/// </summary>
		public IList<Block> Blockchain { get; set; }

		public IList<UnspentTxOut> UnspentTxOuts { get; set; }

		/// <summary>
		/// Generate a new block.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public Block GenerateNextBlock(IList<Transaction> data)
		{
			Block previousBlock = this.Blockchain.GetLatestBlock();
			int difficulty = this.GetDifficulty(this.Blockchain);

			this.logger.LogInformation($"Difficulty: {difficulty}");

			int nextIndex = previousBlock.Index + 1;
			long nextTimestamp = GetCurrentTimestamp();

			Block newBlock = this.FindBlock(nextIndex, data, nextTimestamp, previousBlock.Hash, difficulty);

			this.AddBlock(newBlock);
			this.BroadcastLastest();

			return newBlock;
		}

		/// <summary>
		/// Recplaces the current chain with a new one if the new chaion ist the "longest chain".
		/// With proof-of-work in place we will no longer simply use the longest chain but the one 
		/// with the most cumulative difficulty. The correct chain is the chain which required the 
		/// most resources (= hashRate * time) to produce.
		/// </summary>
		/// <param name="newBlockchain"></param>
		public void ReplaceChain(IList<Block> newBlockchain)
		{
			if (this.IsValidChain(newBlockchain) && GetAccumulatedDifficulty(newBlockchain) > GetAccumulatedDifficulty(this.Blockchain))
			{
				this.logger.LogInformation("Received blockchain is valid. Replacing the current blockchain with the received blockchain.");
				this.Blockchain = newBlockchain;
				this.BroadcastLastest();
			}
			else
			{
				this.logger.LogError("Received blockchain is invalid");
			}
		}

		/// <summary>
		/// Adds a new block to the chain if the block is valid.
		/// </summary>
		/// <param name="newBlock"></param>
		/// <returns></returns>
		public bool AddBlock(Block newBlock)
		{
			if (this.IsValidBlock(newBlock, this.Blockchain.GetLatestBlock()))
			{
				IList<UnspentTxOut> retVal = this.transactionManager.ProcessTransactions(newBlock.Data, this.UnspentTxOuts, newBlock.Index);
				if (retVal == null)
				{
					return false;
				}
				else
				{
					this.Blockchain.Add(newBlock);
					this.UnspentTxOuts = retVal;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Gets a current timestamp in seconds.
		/// </summary>
		/// <returns></returns>
		private static long GetCurrentTimestamp()
		{
			long timestamp = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).Ticks / TimeSpan.TicksPerSecond;
			return timestamp;
		}

		/// <summary>
		/// Finds (mines) a new block for the current difficulty.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="data"></param>
		/// <param name="timestamp"></param>
		/// <param name="previousHash"></param>
		/// <param name="difficulty"></param>
		/// <returns></returns>
		private Block FindBlock(int index, IList<Transaction> data, long timestamp, string previousHash, int difficulty)
		{
			int nonce = 0;

			Stopwatch stopwatch = Stopwatch.StartNew();

			while (true)
			{
				string hash = BlockExtensions.CalculateHash(index, previousHash, timestamp, data, difficulty, nonce);

				if (HashMatchesDifficulty(hash, difficulty))
				{
					stopwatch.Stop();
					this.logger.LogInformation($"Found block. Duration: {stopwatch.ElapsedMilliseconds} ms");

					return new Block(index, data, timestamp, hash, previousHash, difficulty, nonce);
				}

				nonce++;
			}

		}

		/// <summary>
		/// Validates the integrity of a new block.
		/// 
		/// * The index of the block must be one increment larger than the index of the previous block.
		/// * The previous hash of the block must match the hash of the previous block.
		/// * The timestamp of the block must be valid.
		/// * The hash of the block must be valid.
		/// </summary>
		/// <param name="newBlock"></param>
		/// <param name="previousBlock"></param>
		/// <returns></returns>
		private bool IsValidBlock(Block newBlock, Block previousBlock)
		{
			if (previousBlock.Index + 1 != newBlock.Index)
			{
				this.logger.LogError("Invalid index");
				return false;
			}
			else if (previousBlock.Hash != newBlock.PreviousHash)
			{
				this.logger.LogError("Invalid previous hash");
				return false;
			}
			else if (!IsValidTimestamp(newBlock, previousBlock))
			{
				this.logger.LogError("Invalid timestamp");
				return false;
			}
			else if (!this.HasValidHash(newBlock))
			{
				this.logger.LogError("Invalid hash");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validates the integrity of a complete chain.
		/// 
		/// * Check if the first block in the chain matches our genesis block.
		/// * Validate every consecutive block using previous methods.
		/// </summary>
		/// <param name="blockchain"></param>
		/// <returns></returns>
		private bool IsValidChain(IList<Block> blockchain)
		{
			Block genesisBlock = blockchain.FirstOrDefault();

			if (genesisBlock == null || !IsValidGenesisBlock(genesisBlock))
			{
				this.logger.LogError("Invalid genesis block");
				return false;
			}

			for (int i = 1; i < blockchain.Count; i++)
			{
				if (!this.IsValidBlock(blockchain[i], blockchain[i - 1]))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Gets the current difficulty.
		/// </summary>
		/// <param name="blockchain"></param>
		/// <returns></returns>
		private int GetDifficulty(IList<Block> blockchain)
		{
			Block latestBlock = blockchain[this.Blockchain.Count - 1];
			if (latestBlock.Index % Block.DifficultyAdjustmentInterval == 0 && latestBlock.Index != 0)
			{
				return this.GetAdjustedDifficulty(latestBlock, blockchain);
			}
			else
			{
				return latestBlock.Difficulty;
			}
		}

		/// <summary>
		/// Gets the adjusted difficulty if nessessary.
		/// </summary>
		/// <param name="latestBlock"></param>
		/// <param name="blockchain"></param>
		/// <returns></returns>
		private int GetAdjustedDifficulty(Block latestBlock, IList<Block> blockchain)
		{
			Block previousAdjustmentBlock = blockchain[this.Blockchain.Count - Block.DifficultyAdjustmentInterval];
			long timeExpected = Block.BlockGenerationInterval * Block.DifficultyAdjustmentInterval;
			long timeTaken = latestBlock.Timestamp - previousAdjustmentBlock.Timestamp;

			if (timeTaken < timeExpected / 2)
			{
				return previousAdjustmentBlock.Difficulty + 1;
			}
			else if (timeTaken > timeExpected * 2)
			{
				return previousAdjustmentBlock.Difficulty - 1;
			}
			else
			{
				return previousAdjustmentBlock.Difficulty;
			}
		}

		/// <summary>
		/// Gets the accumulated difficulty value to be use in "longest chain" decision. 
		/// </summary>
		/// <param name="blockchain"></param>
		/// <returns></returns>
		private static int GetAccumulatedDifficulty(IList<Block> blockchain)
		{
			int accumulatedDifficulty = (int) blockchain.Select(x => Math.Pow(2, x.Difficulty)).Aggregate((x1, x2) => x1 + x2);
			return accumulatedDifficulty;
		}

		/// <summary>
		/// Validates the timestamp of a block.
		/// 
		/// * A block is valid if the timestamp is at most 1 minute in the future from the time we perceive.
		/// * A block in the chain is valid if the timestamp is at most 1 minute in the past of the previous block.
		/// </summary>
		/// <param name="newBlock"></param>
		/// <param name="previousBlock"></param>
		/// <returns></returns>
		public static bool IsValidTimestamp(Block newBlock, Block previousBlock)
		{
			return (previousBlock.Timestamp - 60 < newBlock.Timestamp) && (newBlock.Timestamp - 60 < GetCurrentTimestamp());
		}

		private bool HasValidHash(Block block)
		{
			if (!HashMatchesBlockContent(block))
			{
				this.logger.LogError($"Invalid hash, got: {block.Hash}");
				return false;
			}

			if (!HashMatchesDifficulty(block.Hash, block.Difficulty))
			{
				this.logger.LogError($"Block difficulty not satisfied. Expected: {block.Difficulty} but got: {block.Hash}");
			}

			return true;
		}

		private static bool HashMatchesBlockContent(Block block)
		{
			string blockHash = block.CalucateHash();
			return blockHash == block.Hash;
		}

		private static bool HashMatchesDifficulty(string hash, int difficulty)
		{
			string hashInBinary = hash.HexToBinary();
			string requiredPrefix = "0".Times(difficulty);

			return hashInBinary.StartsWith(requiredPrefix);
		}

		private static bool IsValidGenesisBlock(Block genesisBlock)
		{
			return JsonConvert.SerializeObject(genesisBlock) == JsonConvert.SerializeObject(Block.Genesis);
		}

		public Task BroadcastLastest()
		{
			return this.broadcastService.BroadcastLastest(this.Blockchain);
		}

		public Task BroadcastQueryAll()
		{
			return this.broadcastService.BroadcastQueryAll();
		}
	}
}