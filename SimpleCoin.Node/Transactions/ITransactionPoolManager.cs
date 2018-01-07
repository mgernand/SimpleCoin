namespace SimpleCoin.Node.Transactions
{
	using System.Collections.Generic;

	public interface ITransactionPoolManager
	{
		IList<Transaction> TransactionPool { get; }

		void AddToTransactionPool(Transaction transaction, IList<UnspentTxOut> unspentTxOuts);

		void UpdateTransactionPool(IList<UnspentTxOut> unspentTxOuts);
	}
}