namespace SimpleCoin.Node.Transactions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using JetBrains.Annotations;
	using Microsoft.Extensions.Logging;
	using Newtonsoft.Json;
	using Util;

	[UsedImplicitly]
	public class TransactionPoolManager : ITransactionPoolManager
	{
		private static readonly object syncRoot = new object();

		private readonly ILogger<TransactionPoolManager> logger;
		private readonly ITransactionManager transactionManager;

		private IList<Transaction> transactionPool;

		public TransactionPoolManager(
			ILogger<TransactionPoolManager> logger, 
			ITransactionManager transactionManager)
		{
			this.logger = logger;
			this.transactionManager = transactionManager;
			this.transactionPool = new List<Transaction>();
		}

		public IList<Transaction> TransactionPool
		{
			get
			{
				lock (syncRoot)
				{
					return this.transactionPool.Clone();
				}
			}
		}

		/// <summary>
		/// Adds the transaction to the transaction pool.
		/// </summary>
		/// <param name="transaction"></param>
		/// <param name="unspentTxOuts"></param>
		public void AddToTransactionPool(Transaction transaction, IList<UnspentTxOut> unspentTxOuts)
		{
			lock (syncRoot)
			{
				if (!this.transactionManager.IsValidTransaction(transaction, unspentTxOuts))
				{
					throw new InvalidOperationException("Trying to add invalid transaction to pool");
				}

				if (!this.IsValidTransactionForPool(transaction))
				{
					throw new InvalidOperationException("Trying to add invalid transaction to pool.");
				}

				this.logger.LogInformation($"Adding transaction to pool: {transaction.Id}");
				this.transactionPool.Add(transaction);
			}
		}

		/// <summary>
		/// Updates the transaction pool.
		/// </summary>
		/// <param name="unspentTxOuts"></param>
		public void UpdateTransactionPool(IList<UnspentTxOut> unspentTxOuts)
		{
			lock (syncRoot)
			{
				IList<Transaction> invalidTransactions = new List<Transaction>();

				foreach (Transaction transaction in this.transactionPool)
				{
					foreach (TxIn txIn in transaction.TxIns)
					{
						if (!HasTxIn(unspentTxOuts, txIn))
						{
							invalidTransactions.Add(transaction);
							break;
						}
					}
				}

				if (invalidTransactions.Count > 0)
				{
					this.logger.LogError(
						$"Removing the following transactions from the pool: {JsonConvert.SerializeObject(invalidTransactions.Select(x => x.Id).ToList())}");

					this.transactionPool = this.transactionPool.Except(invalidTransactions).ToList();
				}
			}
		}

		private IList<TxIn> GetTransactionPoolTxIns()
		{
			return this.transactionPool
					.SelectMany(tx => tx.TxIns)
					.ToList();
		}

		private bool IsValidTransactionForPool(Transaction tx)
		{
			IList<TxIn> txPoolIns = this.GetTransactionPoolTxIns();

			foreach (TxIn txIn in tx.TxIns)
			{
				if (ContainsTxIn(txPoolIns, txIn))
				{
					this.logger.LogError("TxIn already found in the transaction pool");
					return false;
				}
			}

			return true;
		}

		private static bool ContainsTxIn(IList<TxIn> txPoolIns, TxIn txIn)
		{
			return txPoolIns.Any(txPoolIn => txIn.TxOutIndex == txPoolIn.TxOutIndex && txIn.TxOutId == txPoolIn.TxOutId);
		}

		private static bool HasTxIn(IList<UnspentTxOut> unspentTxOuts, TxIn txIn)
		{
			return unspentTxOuts.Any(uTxOut => uTxOut.TxOutId == txIn.TxOutId && uTxOut.TxOutIndex == txIn.TxOutIndex);
		}
	}
}