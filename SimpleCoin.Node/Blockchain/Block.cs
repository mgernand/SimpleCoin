namespace SimpleCoin.Node.Blockchain
{
	using System;
	using System.Linq;
	using System.Security.Cryptography;
	using System.Text;

	public sealed class Block
	{
		public Block(ulong index, string data, DateTime timestamp, string previousHash)
		{
			this.Index = index;
			this.Data = data ?? string.Empty;
			this.Timestamp = timestamp;
			this.PreviousHash = previousHash ?? string.Empty;

			// Create the SHA256 hash for the block.
			this.Hash = BlockExtensions.CalculateHash(index, previousHash, timestamp, data);
		}

		/// <summary>
		/// The height of the block in the chain.
		/// </summary>
		public ulong Index { get; }

		/// <summary>
		/// The data included in the block.
		/// </summary>
		public string Data { get; }

		/// <summary>
		/// The creation timestamp of the block.
		/// </summary>
		public DateTime Timestamp { get; }

		/// <summary>
		/// A SHA256 hash taken from the content of the block.
		/// </summary>
		public string Hash { get; }

		/// <summary>
		/// The reference to the hash of the previous block. This value explicitly references the previous block.
		/// </summary>
		public string PreviousHash { get; }

		/// <summary>
		/// The hard-coded genesis block of the blockchain.
		/// </summary>
		public static Block Genesis { get; } = new Block(0, null, DateTime.Parse("01.01.2018 00:00:00"), null);

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format(
				$"[Block: Index={this.Index}, Hash={this.Hash}. TimeStamp={this.Timestamp:O}], PreviousHash={this.PreviousHash}, Data={this.Data}]");
		}
	}
}