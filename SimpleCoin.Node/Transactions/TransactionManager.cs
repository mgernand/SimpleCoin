namespace SimpleCoin.Node.Transactions
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using JetBrains.Annotations;
	using Microsoft.Extensions.Logging;
	using Newtonsoft.Json;
	using Util;

	[UsedImplicitly]
	public class TransactionManager
	{
		private readonly ILogger<TransactionManager> logger;

		public TransactionManager(ILogger<TransactionManager> logger)
		{
			this.logger = logger;
		}

		/// <summary>
		/// Calculates the tranaction id by creating a hash from the contents of the tranaction.
		/// The signatures of the TxIns are not included in the transaction hash, because they
		/// are added later on.
		/// </summary>
		/// <param name="transaction"></param>
		/// <returns></returns>
		private static string GetTransactionId(Transaction transaction)
		{
			string txInContent = transaction.TxIns
				.Select(x => x.TxOutId + x.TxOutIndex.ToString())
				.Aggregate((s1, s2) => s1 + s1);

			string txOutContent = transaction.TxOuts
				.Select(x => x.Address + x.Amount.ToString(CultureInfo.InvariantCulture))
				.Aggregate((s1, s2) => s1 + s2);

			return (txInContent + txOutContent).CalculateHash();
		}


		private bool IsValidTransaction(Transaction transaction, IList<UnspentTxOut> unspentTxOuts)
		{
			if (GetTransactionId(transaction) != transaction.Id)
			{
				this.logger.LogError($"Invalid transaction id: {transaction.Id}");
				return false;
			}

			bool hasValidTxIns = transaction.TxIns
				.Select(txIn => this.IsValidTxIn(txIn, transaction, unspentTxOuts))
				.Aggregate((a, b) => a && b);

			if (!hasValidTxIns)
			{
				this.logger.LogError($"Some of the txIns are invalid in transaction: {transaction.Id}");
				return false;
			}

			int totalTxInValues = transaction.TxIns
				.Select(txIn => this.GetTxInAmount(txIn, unspentTxOuts))
				.Aggregate((a, b) => a + b);

			int totalTxOutValues = transaction.TxOuts
				.Select(txOut => txOut.Amount)
				.Aggregate((a, b) => a + b);

			if (totalTxOutValues != totalTxInValues)
			{
				this.logger.LogError($"Total txOut values != total txIn values in transaction: {transaction.Id}");
				return false;
			}

			return true;
		}

		private bool ValidateBlockTransactions(IList<Transaction> transactions, IList<UnspentTxOut> unspentTxOuts, int blockIndex)
		{
			Transaction coinbaseTx = transactions[0];
			if (!this.IsValidCoinbaseTransaction(coinbaseTx, blockIndex))
			{
				this.logger.LogError($"Invalid coinbase transaction: {JsonConvert.SerializeObject(coinbaseTx)}");
				return false;
			}

			// Check for duplicate txIns. Each txIn can be included only once.
			IList<TxIn> txIns = transactions.SelectMany(tx => tx.TxIns).ToList();

			if (this.HasDuplicates(txIns))
			{
				this.logger.LogError("Found duplicated txIns");
				return false;
			}

			// All but coinbase transactions
			IList<Transaction> normalTransactions = transactions.Skip(1).ToList();

			if (normalTransactions.Any())
			{
				return normalTransactions
					.Select(tx => this.IsValidTransaction(tx, unspentTxOuts))
					.Aggregate((a, b) => a && b);
			}

			return true;
		}

		private bool HasDuplicates(IList<TxIn> txIns)
		{
			List<string> duplicateKeys = txIns.GroupBy(txIn => txIn.TxOutId)
				.Where(group => group.Count() > 1)
				.Select(group => group.Key)
				.ToList();

			return duplicateKeys.Count > 0;
		}

		private bool IsValidCoinbaseTransaction(Transaction transaction, int blockIndex)
		{
			if (transaction == null)
			{
				this.logger.LogError("The first transaction in the block must be a coinbase transaction");
				return false;
			}

			if (GetTransactionId(transaction) != transaction.Id)
			{
				this.logger.LogError($"Invalid coinbase transaction id: {transaction.Id}");
				return false;
			}

			if (transaction.TxIns.Count != 1)
			{
				this.logger.LogError("One txIn must be specified in the coinbase transaction");
				return false;
			}

			if (transaction.TxIns[0].TxOutIndex != blockIndex)
			{
				this.logger.LogError("The txIn signature in coinbase tx must be the block height");
				return false;
			}

			if (transaction.TxOuts.Count != 1)
			{
				this.logger.LogError("Invalid number of txOuts in coinbase transaction");
				return false;
			}

			if (transaction.TxOuts[0].Amount != Transaction.CoinbaseAmount)
			{
				this.logger.LogError("Invalid coinbase amount in coinbase transaction");
				return false;
			}

			return true;
		}

		private int GetTxInAmount(TxIn txIn, IList<UnspentTxOut> unspentTxOuts)
		{
			return FindUnspentTxOut(txIn.TxOutId, txIn.TxOutIndex, unspentTxOuts).Amount;
		}

		private static UnspentTxOut FindUnspentTxOut(string transactionId, int index, IList<UnspentTxOut> unspentTxOuts)
		{
			return unspentTxOuts.FirstOrDefault(uTxOut => uTxOut.TxOutId == transactionId && uTxOut.TxOutIndex == index);
		}

		private Transaction GetCoinbaseTransaction(string address, int blockIndex)
		{
			Transaction transaction = new Transaction();
			TxIn txIn = new TxIn
			{
				Signature = "", 
				TxOutId = "",
				TxOutIndex = blockIndex,
			};

			transaction.TxIns.Add(txIn);
			transaction.TxOuts.Add(new TxOut(address, Transaction.CoinbaseAmount));
			transaction.Id = GetTransactionId(transaction);

			return transaction;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="txIn"></param>
		/// <param name="transaction"></param>
		/// <param name="unspentTxOuts"></param>
		/// <returns></returns>
		private bool IsValidTxIn(TxIn txIn, Transaction transaction, IList<UnspentTxOut> unspentTxOuts)
		{
			UnspentTxOut referencedUnspentTxOut = unspentTxOuts
				.FirstOrDefault(uTxOut => uTxOut.TxOutId == txIn.TxOutId && uTxOut.TxOutIndex == txIn.TxOutIndex);

			if (referencedUnspentTxOut == null)
			{
				this.logger.LogError($"Referenced txOut not found: {JsonConvert.SerializeObject(txIn)}");
				return false;
			}

			string address = referencedUnspentTxOut.Address;
			bool isValidSignature = Crypto.VerifySignature(address, transaction.Id, txIn.Signature);
			if (!isValidSignature)
			{
				this.logger.LogError($"Invalid txIn signature: {txIn.Signature} txId: {transaction.Id} address: {address}");
			}

			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="transaction"></param>
		/// <param name="txInIndex"></param>
		/// <param name="privateKey"></param>
		/// <param name="unspentTxOuts"></param>
		/// <returns></returns>
		private string SignTxIn(Transaction transaction, int txInIndex, string privateKey, IList<UnspentTxOut> unspentTxOuts)
		{
			TxIn txIn = transaction.TxIns[txInIndex];

			string dataToSign = transaction.Id;
			UnspentTxOut referencedUnspentTxOut = FindUnspentTxOut(txIn.TxOutId, txIn.TxOutIndex, unspentTxOuts);

			if (referencedUnspentTxOut == null)
			{
				this.logger.LogError("Could not find referenced txOut");
				throw new InvalidOperationException("Could not find referenced txOut");
			}

			string referencedAddress = referencedUnspentTxOut.Address;

			if (Crypto.GetPublicKey(privateKey) != referencedAddress)
			{
				this.logger.LogError("Trying to sign an input with private key that does not match the address that is referenced in txIn.");
				throw new InvalidOperationException("Trying to sign an input with private key that does not match the address that is referenced in txIn.");
			}

			string signature = Crypto.GetSignature(dataToSign, privateKey);
			return signature;
		}

		private IList<UnspentTxOut> UpdateUnspentTxOuts(IList<Transaction> newTransactions, IList<UnspentTxOut> unspentTxOuts)
		{
			IList<UnspentTxOut> newUnspentTxOuts = newTransactions
				.Select(tx =>
				{
					return tx.TxOuts.Select((txOut, index) => new UnspentTxOut(tx.Id, index, txOut.Address, txOut.Amount));
				})
				.Aggregate((a, b) => a.Concat(b)).ToList();

			IList<UnspentTxOut> consumedTxOuts = newTransactions
				.Select(tx => tx.TxIns)
				.Aggregate((a, b) => (IList<TxIn>) a.Concat(b))
				.Select(txIn => new UnspentTxOut(txIn.TxOutId, txIn.TxOutIndex, string.Empty, 0)).ToList();

			IList<UnspentTxOut> resultingUnspentTxOuts = unspentTxOuts
				.Where(uTxOut => FindUnspentTxOut(uTxOut.TxOutId, uTxOut.TxOutIndex, consumedTxOuts) != null)
				.Concat(newUnspentTxOuts)
				.ToList();

			return resultingUnspentTxOuts;
		}

		public IList<UnspentTxOut> ProcessTransactions(IList<Transaction> transactions, IList<UnspentTxOut> unspentTxOuts, int blockIndex)
		{
			if (!this.ValidateBlockTransactions(transactions, unspentTxOuts, blockIndex))
			{
				this.logger.LogError("Invalid block transactions");
				return null;
			}

			return this.UpdateUnspentTxOuts(transactions, unspentTxOuts);
		}

		///// <summary>
		///// Valid address is a valid ecdsa public key in the 04 + X-coordinate + Y-coordinate format.
		///// </summary>
		///// <param name="address"></param>
		///// <returns></returns>
		//private bool IsValidAddress(string address)
		//{
		//	//if (address.Length !== 130)
		//	//{
		//	//	this.logger.LogError("Invalid public key length");
		//	//	return false;
		//	//}
		//	//else if (address.match('^[a-fA-F0-9]+$') === null)
		//	//{
		//	//	console.log('public key must contain only hex characters');
		//	//	return false;
		//	//}
		//	//else if (!address.startsWith('04'))
		//	//{
		//	//	console.log('public key must start with 04');
		//	//	return false;
		//	//}
		//	return true;
		//}
	}
}