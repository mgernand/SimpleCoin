namespace SimpleCoin.Node.Transactions
{
	using System;

	/// <summary>
	/// Unspent transaction outputs that make up the balance of a wallet.
	/// </summary>
	public sealed class UnspentTxOut : ICloneable
	{
		public UnspentTxOut(string txOutId, int txOutIndex, string address, long amount)
		{
			this.TxOutId = txOutId;
			this.TxOutIndex = txOutIndex;
			this.Address = address;
			this.Amount = amount;
		}

		public string TxOutId { get; }

		public int TxOutIndex { get; }

		public string Address { get; }

		public long Amount { get; }

		public TxIn ToUnsignedTxIn()
		{
			TxIn txIn = new TxIn
			{
				TxOutId = this.TxOutId,
				TxOutIndex = this.TxOutIndex
			};
			return txIn;
		}

		/// <inheritdoc />
		public object Clone()
		{
			return new UnspentTxOut(this.TxOutId, this.TxOutIndex, this.Address, this.Amount);
		}
	}
}