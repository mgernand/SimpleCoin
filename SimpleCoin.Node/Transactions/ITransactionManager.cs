namespace SimpleCoin.Node.Transactions
{
	using System.Collections.Generic;

	public interface ITransactionManager
	{
		bool IsValidAddress(string address);

		bool IsValidTransaction(Transaction transaction, IList<UnspentTxOut> unspentTxOuts);

		IList<UnspentTxOut> ProcessTransactions(IList<Transaction> transactions, IList<UnspentTxOut> unspentTxOuts, int blockIndex);

		string SignTxIn(Transaction transaction, int txInIndex, string privateKey, IList<UnspentTxOut> unspentTxOuts);
	}
}