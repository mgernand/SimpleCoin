namespace SimpleCoin.Node.Wallet
{
	using System.Collections.Generic;
	using SimpleCoin.Node.Transactions;

	public interface IWalletManager
	{
		Transaction CreateTransaction(string receiverAdress, long amount, string privateKey, IList<UnspentTxOut> unspentTxOuts, IList<Transaction> transactionPool);

		void DeleteWallet();

		long GetBalance(string address, IList<UnspentTxOut> unspentTxOuts);

		string GetPrivateKeyFromWallet();

		string GetPublicKeyFromWallet();

		void InitWallet();
	}
}