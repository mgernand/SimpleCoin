namespace SimpleCoin.Node.Blockchain
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using JetBrains.Annotations;
	using Microsoft.Extensions.Logging;
	using PeerToPeer;

	[UsedImplicitly]
	public class BlockchainManager
	{
		private readonly ILogger<BlockchainManager> logger;
		private readonly BroadcastService broadcastService;

		public BlockchainManager(ILogger<BlockchainManager> logger, BroadcastService broadcastService)
		{
			this.logger = logger;
			this.broadcastService = broadcastService;

			this.Blockchain = new List<Block> { Block.Genesis };
		}


		/// <summary>
		/// In-memory stored blockchain.
		/// </summary>
		public IList<Block> Blockchain { get; set; }

		/// <summary>
		/// Generate a new block.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public Block GenerateNextBlock(string data)
		{
			Block previousBlock = this.Blockchain.GetLatestBlock();
			ulong nextIndex = previousBlock.Index + 1;
			DateTime nextTimestamp = DateTime.UtcNow;
			
			Block newBlock = new Block(nextIndex, data, nextTimestamp, previousBlock.Hash);
			this.AddBlock(newBlock);
			this.BroadcastLastest();

			return newBlock;
		}

		/// <summary>
		/// Validates the integrity of a new block.
		/// 
		/// * The index of the block must be one increment larger than the index of the previous block.
		/// * The previous hash of the block must match the hash of the previous block.
		/// * The hash of the block must be valid.
		/// </summary>
		/// <param name="newBlock"></param>
		/// <param name="previousBlock"></param>
		/// <returns></returns>
		public bool IsValidBlock(Block newBlock, Block previousBlock)
		{
			if (previousBlock.Index + 1 != newBlock.Index)
			{
				this.logger.LogError("Block validation error: invalid index");
				return false;
			}
			else if (previousBlock.Hash != newBlock.PreviousHash)
			{
				this.logger.LogError("Block validation error: invalid previous hash");
				return false;
			}
			else if(newBlock.CalucateHash() != newBlock.Hash)
			{
				this.logger.LogError($"Block validation error: invalid hash: {newBlock.CalucateHash()} {newBlock.Hash}");
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
		public bool IsValidChain(IList<Block> blockchain)
		{
			Block genesisBlock = blockchain.FirstOrDefault();

			if (genesisBlock == null || !IsValidGenesisBlock(genesisBlock))
			{
				this.logger.LogError("Blockchain validation: invalid genesis block");
				return false;
			}

			for (int i = 1; i < blockchain.Count; i++)
			{
				if(!this.IsValidBlock(blockchain[i], blockchain[i - 1]))
				{
					return false;
				}
			}

			return true;
		}

		public void ReplaceChain(IList<Block> newBlockchain)
		{
			if (this.IsValidChain(newBlockchain) && newBlockchain.Count > this.Blockchain.Count)
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

		public bool AddBlock(Block newBlock)
		{
			if (this.IsValidBlock(newBlock, this.Blockchain.GetLatestBlock()))
			{
				this.Blockchain.Add(newBlock);
				return true;
			}
			return false;
		}

		private static bool IsValidGenesisBlock(Block genesisBlock)
		{
			return genesisBlock.Index == Block.Genesis.Index &&
				   genesisBlock.Hash == Block.Genesis.Hash &&
				   genesisBlock.PreviousHash == Block.Genesis.PreviousHash &&
				   genesisBlock.Timestamp == Block.Genesis.Timestamp &&
				   genesisBlock.Data == Block.Genesis.Data;
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