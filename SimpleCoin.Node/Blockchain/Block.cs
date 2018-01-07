namespace SimpleCoin.Node.Blockchain
{
	using System;
	using System.Collections.Generic;
	using Transactions;

	public sealed class Block
	{
		// In seconds.
		public const int BlockGenerationInterval = 10;

		// In blocks.
		public const int DifficultyAdjustmentInterval = 10;

		public Block(int index, IList<Transaction> data, long timestamp, string hash, string previousHash, int difficulty, int nonce)
		{
			this.Index = index;
			this.Data = data ?? new List<Transaction>();
			this.Timestamp = timestamp;
			this.Hash = hash;
			this.PreviousHash = previousHash ?? string.Empty;
			this.Difficulty = difficulty;
			this.Nonce = nonce;
		}

		/// <summary>
		/// The height of the block in the chain.
		/// </summary>
		public int Index { get; }

		/// <summary>
		/// The data included in the block.
		/// </summary>
		public IList<Transaction> Data { get; }

		/// <summary>
		/// The creation timestamp of the block (seconds since 01.01.1970, UNIX epoch).
		/// </summary>
		public long Timestamp { get; }

		/// <summary>
		/// A SHA256 hash taken from the content of the block.
		/// </summary>
		public string Hash { get; }

		/// <summary>
		/// The reference to the hash of the previous block. This value explicitly references the previous block.
		/// </summary>
		public string PreviousHash { get; }

		/// <summary>
		/// The difficulty defines how many prefixing zeros the block hash must have to be valid.
		/// The prefixing zeros are checked in the bianry format of the hash.
		/// </summary>
		public int Difficulty { get; }

		/// <summary>
		/// Nonce to differ the block hash when finding the next block.
		/// </summary>
		public int Nonce { get; }

		/// <summary>
		/// The hard-coded genesis block of the blockchain.
		/// </summary>
		public static Block Genesis { get; } = new Block(0, new List<Transaction> { Transaction.Genesis }, 1465154705, "d7bf36b454d358d7a73eec6e365d16669a32e9f3ca5f9d216112bd9d391230af", null, 0, 0);

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format(
				$"[Block: Index={this.Index}, Hash={this.Hash}. TimeStamp={this.Timestamp:O}], PreviousHash={this.PreviousHash}, Data={this.Data}]");
		}
	}
}