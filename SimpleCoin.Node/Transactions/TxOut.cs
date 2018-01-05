namespace SimpleCoin.Node.Transactions
{
	/// <summary>
	/// Transaction outputs (txOut) consists of an address and an amount of coins. 
	/// The address is an ECDSA public-key. This means that the user having the 
	/// private-key of the referenced public-key (=address) will be able to access 
	/// the coins.
	/// </summary>
	public class TxOut
	{
		public TxOut(string address, long amount)
		{
			this.Address = address;
			this.Amount = amount;
		}

		public string Address { get; }

		public long Amount { get; }
	}
}