namespace SimpleCoin.Node.Transactions
{
	using System.Collections.Generic;

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
	}
}