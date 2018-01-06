namespace SimpleCoin.Node.Transactions
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using Util;

	public class Transaction : ICloneable
	{
		public const int CoinbaseAmount = 50;

		public Transaction()
		{
			this.TxIns = new List<TxIn>();
			this.TxOuts = new List<TxOut>();
		}

		/// <summary>
		/// The id of the transaction.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// The transaction inputs.
		/// </summary>
		public IList<TxIn> TxIns { get; set; }

		/// <summary>
		/// The transaction outputs.
		/// </summary>
		public IList<TxOut> TxOuts { get; set; }

		/// <summary>
		/// Calculates the tranaction id by creating a hash from the contents of the tranaction.
		/// The signatures of the TxIns are not included in the transaction hash, because they
		/// are added later on.
		/// </summary>
		/// <param name="transaction"></param>
		/// <returns></returns>
		public static string GetTransactionId(Transaction transaction)
		{
			string txInContent = transaction.TxIns
				.Select(x => x.TxOutId + x.TxOutIndex.ToString())
				.Aggregate((s1, s2) => s1 + s1);

			string txOutContent = transaction.TxOuts
				.Select(x => x.Address + x.Amount.ToString(CultureInfo.InvariantCulture))
				.Aggregate((s1, s2) => s1 + s2);

			return (txInContent + txOutContent).CalculateHash();
		}

		public static Transaction Genesis { get; } = new Transaction
		{
			TxIns = new List<TxIn> { new TxIn { Signature = "", TxOutId = "", TxOutIndex = 0 }},
			TxOuts = new List<TxOut> { new TxOut("04bfcab8722991ae774db48f934ca79cfb7dd991229153b9f732ba5334aafcd8e7266e47076996b55a14bf9913ee3145ce0cfc1372ada8ada74bd287450313534a", 50) },
			Id = "e655f6a5f26dc9b4cac6e46f52336428287759cf81ef5ff10854f69d68f43fa3"
		};

		/// <inheritdoc />
		public object Clone()
		{
			return new Transaction
			{
				TxIns = this.TxIns.Select(tx => (TxIn)tx.Clone()).ToList(),
				TxOuts = this.TxOuts.Select(tx => (TxOut)tx.Clone()).ToList(),
				Id = this.Id,
			};
		}
	}
}