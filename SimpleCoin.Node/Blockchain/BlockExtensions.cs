namespace SimpleCoin.Node.Blockchain
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Security.Cryptography;
	using System.Text;

	public static class BlockExtensions
	{
		public static Block GetLatestBlock(this IList<Block> blockchain)
		{
			return blockchain.Last();
		}

		public static string CalucateHash(this Block block)
		{
			return CalculateHash(block.Index, block.PreviousHash, block.Timestamp, block.Data);
		}

		/// <summary>
		/// Calculate a SHA256 hash for the given string values.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="previousHash"></param>
		/// <param name="timestamp"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public static string CalculateHash(ulong index, string previousHash, DateTime timestamp, string data)
		{
			return CalculateHash(index.ToString(), previousHash, timestamp.ToString("O"), data);
		}

		private static string CalculateHash(params string[] args)
		{
			string str = args.Aggregate((s1, s2) => s1 + s2);
			byte[] bytes = Encoding.UTF8.GetBytes(str);

			string hash = string.Empty;
			byte[] hashBytes = new SHA256Managed().ComputeHash(bytes);
			foreach (byte x in hashBytes)
			{
				hash += string.Format("{0:x2}", x);
			}

			return hash;
		}
	}
}