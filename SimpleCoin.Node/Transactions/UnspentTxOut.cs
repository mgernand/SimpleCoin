namespace SimpleCoin.Node.Transactions
{
	public class UnspentTxOut
	{
		public UnspentTxOut(string txOutId, int txOutIndex, string address, int amount)
		{
			this.TxOutId = txOutId;
			this.TxOutIndex = txOutIndex;
			this.Address = address;
			this.Amount = amount;
		}

		public string TxOutId { get; }

		public int TxOutIndex { get; }

		public string Address { get; }

		public int Amount { get; }
	}
}