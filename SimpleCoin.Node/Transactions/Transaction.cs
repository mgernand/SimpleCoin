namespace SimpleCoin.Node.Transactions
{
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using Util;

	public class Transaction
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
	}
}