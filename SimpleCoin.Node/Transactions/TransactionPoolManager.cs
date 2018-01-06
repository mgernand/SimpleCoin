namespace SimpleCoin.Node.Transactions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using JetBrains.Annotations;
	using Microsoft.Extensions.Logging;
	using Newtonsoft.Json;

	[UsedImplicitly]
	public class TransactionPoolManager
	{
		private readonly ILogger<TransactionPoolManager> logger;
		private readonly TransactionManager transactionManager;

		private readonly IList<Transaction> transactionPool;

		public TransactionPoolManager(ILogger<TransactionPoolManager> logger, TransactionManager transactionManager)
		{
			this.logger = logger;
			this.transactionManager = transactionManager;
			this.transactionPool = new List<Transaction>();
		}

		public IList<Transaction> TransactionPool
		{
			get { return this.transactionPool.Select(tx => (Transaction)tx.Clone()).ToList(); }
		}

		public void AddToTransactionPool(Transaction tx, IList<UnspentTxOut> unspentTxOuts)
		{
			if (!this.transactionManager.IsValidTransaction(tx, unspentTxOuts))
			{
				throw new InvalidOperationException("Trying to add invalid transaction to pool");
			}

			if (!this.IsValidTransactionForPool(tx, this.transactionPool))
			{
				throw new InvalidOperationException("Trying to add invalid transaction to pool.");
			}

			this.logger.LogInformation($"Adding transaction to pool: {tx.Id}");
		}

		public void UpdateTransactionPool(IList<UnspentTxOut> unspentTxOuts)
		{
			IList<Transaction> invalidTransactions = new List<Transaction>();

			foreach (Transaction transaction in this.transactionPool)
			{
				foreach (TxIn txIn in transaction.TxIns)
				{
					if (!this.HasTxIn(txIn, unspentTxOuts))
					{
						invalidTransactions.Add(transaction);
						break;
					}
				}
			}

			if (invalidTransactions.Count > 0)
			{
				this.logger.LogError($"Removing the following transactions from the pool: {JsonConvert.SerializeObject(invalidTransactions.Select(x => x.Id).ToList())}");
			}
		}

		private IList<TxIn> GetTransactionPoolTxIns(IList<Transaction> aTransactionPool)
		{
			return aTransactionPool
				.SelectMany(tx => tx.TxIns)
				.ToList();
		}

		private bool IsValidTransactionForPool(Transaction tx, IList<Transaction> aTransactionPool)
		{
			IList<TxIn> txPoolIns = this.GetTransactionPoolTxIns(aTransactionPool);

			foreach (TxIn txIn in tx.TxIns)
			{
				if (this.ContainsTxIn(txIn, txPoolIns))
				{
					this.logger.LogError("TxIn already found in the transaction pool");
					return false;
				}
			}

			return true;
		}

		private bool ContainsTxIn(TxIn txIn, IList<TxIn> txPoolIns)
		{
			return txPoolIns.Any(txPoolIn => txIn.TxOutIndex == txPoolIn.TxOutIndex && txIn.TxOutId == txPoolIn.TxOutId);
		}

		private bool HasTxIn(TxIn txIn, IList<UnspentTxOut> unspentTxOuts)
		{
			return unspentTxOuts.Any(uTxOut => uTxOut.TxOutId == txIn.TxOutId && uTxOut.TxOutIndex == txIn.TxOutIndex);
		}
	}
}