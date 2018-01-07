namespace SimpleCoin.Node.PeerToPeer
{
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using SimpleCoin.Node.Blockchain;
	using SimpleCoin.Node.Transactions;

	public interface IBroadcastService
	{
		Task BroadcastLastest(IList<Block> blockchain);

		Task BroadcastMessage(Message message);

		Task BroadcastQueryAll();

		Task BroadcastTransactionPool(IList<Transaction> transactionPool);
	}
}