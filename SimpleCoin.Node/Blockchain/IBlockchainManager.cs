namespace SimpleCoin.Node.Blockchain
{
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using SimpleCoin.Node.Transactions;

	public interface IBlockchainManager
	{
		IList<Block> Blockchain { get; }

		IList<UnspentTxOut> UnspentTxOuts { get; }

		bool AddBlockToChain(Block newBlock);

		Task BroadcastLastest();

		Task BroadcastQueryAll();

		Block GenerateNextBlock();

		Block GenerateNextBlockWithTransaction(string receiverAddress, long amount);

		Block GenerateRawNextBlock(IList<Transaction> blockData);

		long GetAccountBalance();

		long GetAccountBalance(string address);

		IList<UnspentTxOut> GetUnspentTxOuts(string address);

		void ReplaceChain(IList<Block> newBlockchain);

		Transaction SendTransaction(string receiverAddress, long amount);
	}
}