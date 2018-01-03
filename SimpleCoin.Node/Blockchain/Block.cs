namespace SimpleCoin.Node.Blockchain
{
	using System;

	public sealed class Block
	{
		// In seconds.
		public const int BlockGenerationInterval = 10;

		// In blocks.
		public const int DifficultyAdjustmentInterval = 10;

		public Block(long index, string data, long timestamp, string previousHash, int difficulty, int nonce)
		{
			this.Index = index;
			this.Data = data ?? string.Empty;
			this.Timestamp = timestamp;
			this.PreviousHash = previousHash ?? string.Empty;
			this.Difficulty = difficulty;
			this.Nonce = nonce;

			// Create the SHA256 hash for the block.
			this.Hash = BlockExtensions.CalculateHash(index, previousHash, timestamp, data, difficulty, nonce);
		}

		/// <summary>
		/// The height of the block in the chain.
		/// </summary>
		public long Index { get; }

		/// <summary>
		/// The data included in the block.
		/// </summary>
		public string Data { get; }

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
		/// 
		/// </summary>
		public int Nonce { get; }

		/// <summary>
		/// The hard-coded genesis block of the blockchain.
		/// </summary>
		public static Block Genesis { get; } = new Block(0, null, 1465154705, null, 0, 0);

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format(
				$"[Block: Index={this.Index}, Hash={this.Hash}. TimeStamp={this.Timestamp:O}], PreviousHash={this.PreviousHash}, Data={this.Data}]");
		}
	}
}