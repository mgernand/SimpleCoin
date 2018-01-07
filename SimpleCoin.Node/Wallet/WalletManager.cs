namespace SimpleCoin.Node.Wallet
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using JetBrains.Annotations;
	using Microsoft.Extensions.Logging;
	using Microsoft.Extensions.Options;
	using Transactions;

	[UsedImplicitly]
	public class WalletManager : IWalletManager
	{
		private readonly ILogger<WalletManager> logger;
		private readonly IOptions<ApplicationSettings> options;
		private readonly ITransactionManager transactionManager;

		public WalletManager(
			ILogger<WalletManager> logger, 
			IOptions<ApplicationSettings> options,
			ITransactionManager transactionManager)
		{
			this.logger = logger;
			this.options = options;
			this.transactionManager = transactionManager;
		}

		/// <summary>
		/// Initializes a wallet for the running node. 
		/// The wallet will be used as the default for the running node.
		/// </summary>
		public void InitWallet()
		{
			string path = Path.Combine("node", this.options.Value.Port.ToString(), "wallet");
			string privateKeyFilePath = Path.Combine(path, "private_key");

			// Create the output directory if it does not exist already.
			Directory.CreateDirectory(path);

			// Let's not overwrite existing keys.
			if (File.Exists(privateKeyFilePath))
			{
				return;
			}
			
			string newPrivateKey = Crypto.GeneratePrivateKey();

			// NOTE: We use the nodes port to distiguish between different nodes running locally.
			using (FileStream fileStream = File.Create(privateKeyFilePath))
			using (StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8))
			{
				writer.Write(newPrivateKey);
			}

			this.logger.LogInformation("New wallet with private key created.");
		}

		public void DeleteWallet()
		{
			string path = Path.Combine("node", this.options.Value.Port.ToString(), "wallet");
			string privateKeyFilePath = Path.Combine(path, "private_key");

			if (File.Exists(privateKeyFilePath))
			{
				File.Delete(privateKeyFilePath);
			}
		}

		/// <summary>
		/// Loads the private key from the key file.
		/// </summary>
		/// <returns></returns>
		public string GetPrivateKeyFromWallet()
		{
			// NOTE: We use the nodes port to distiguish between different nodes running locally.
			string privateKey = File.ReadAllText($"node/{this.options.Value.Port}/wallet/private_key", Encoding.UTF8);
			return privateKey.Trim();
		}

		/// <summary>
		/// Gets the public key.
		/// </summary>
		/// <returns></returns>
		public string GetPublicKeyFromWallet()
		{
			string privateKey = this.GetPrivateKeyFromWallet();
			string publicKey = Crypto.GetPublicKey(privateKey);

			return publicKey;
		}

		/// <summary>
		/// Calculates the balance of the account.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="unspentTxOuts"></param>
		/// <returns></returns>
		public long GetBalance(string address, IList<UnspentTxOut> unspentTxOuts)
		{
			return unspentTxOuts
				.Where(uTxOut => uTxOut.Address == address)
				.Select(uTxOut => uTxOut.Amount)
				.Sum();
		}

		/// <summary>
		/// Creates a signed transaction.
		/// </summary>
		/// <param name="receiverAdress"></param>
		/// <param name="amount"></param>
		/// <param name="privateKey"></param>
		/// <param name="unspentTxOuts"></param>
		/// <returns></returns>
		public Transaction CreateTransaction(string receiverAdress, long amount, string privateKey, IList<UnspentTxOut> unspentTxOuts, IList<Transaction> transactionPool)
		{
			this.logger.LogInformation($"Transaction pool size: {transactionPool.Count}");

			string myAddress = Crypto.GetPublicKey(privateKey);
			IList<UnspentTxOut> myUnspentTxOutsA = unspentTxOuts.Where(uTxOut => uTxOut.Address == myAddress).ToList();
			IList<UnspentTxOut> myUnspentTxOuts = FilterTransactionPoolTransactions(myUnspentTxOutsA, transactionPool);

			// Filter from unspentOutputs such inputs that are referenced in pool.
			(IList<UnspentTxOut> includedUnspentTxOuts, long leftOverAmount) = FindTxOutsForAmount(amount, myUnspentTxOuts);

			IList<TxIn> unsignedTxIns = includedUnspentTxOuts.Select(uTxOut => uTxOut.ToUnsignedTxIn()).ToList();

			Transaction tx = new Transaction
			{
				TxIns = unsignedTxIns,
				TxOuts = CreateTxOuts(receiverAdress, myAddress, amount, leftOverAmount)
			};
			tx.Id = Transaction.GetTransactionId(tx);

			tx.TxIns = tx.TxIns.Select((txIn, index) =>
			{
				txIn.Signature = this.transactionManager.SignTxIn(tx, index, privateKey, unspentTxOuts);
				return txIn;
			}).ToList();

			return tx;
		}

		public static IList<UnspentTxOut> FindUnspentTxOuts(string ownerAddress, IList<UnspentTxOut> unspentTxOuts)
		{
			return unspentTxOuts.Where(uTxOut => uTxOut.Address == ownerAddress).ToList();
		}

		private static IList<UnspentTxOut> FilterTransactionPoolTransactions(IList<UnspentTxOut> unspentTxOuts, IList<Transaction> transactionPool)
		{
			IList<TxIn> txIns = transactionPool
				.SelectMany(tx => tx.TxIns)
				.ToList();

			IList<UnspentTxOut> removable = new List<UnspentTxOut>();

			foreach (UnspentTxOut unspentTxOut in unspentTxOuts)
			{
				TxIn txIn = txIns.FirstOrDefault(aTxIn =>aTxIn.TxOutIndex == unspentTxOut.TxOutIndex && aTxIn.TxOutId == unspentTxOut.TxOutId);

				if (txIn != null)
				{
					removable.Add(unspentTxOut);
				}
			}

			return unspentTxOuts.Except(removable).ToList();
		}

		private static (IList<UnspentTxOut>, long) FindTxOutsForAmount(long amount, IList<UnspentTxOut> myUnspentTxOuts)
		{
			long currentAmount = 0;
			IList<UnspentTxOut> includedUnspentTxOuts = new List<UnspentTxOut>();

			foreach (UnspentTxOut myUnspentTxOut in myUnspentTxOuts)
			{
				includedUnspentTxOuts.Add(myUnspentTxOut);
				currentAmount += myUnspentTxOut.Amount;

				if (currentAmount >= amount)
				{
					long leftOverAmount = currentAmount - amount;
					return (includedUnspentTxOuts, leftOverAmount);
				}
			}

			throw new InvalidOperationException("Not enough coins to send in transaction.");
		}

		private static IList<TxOut> CreateTxOuts(string receiverAddress, string myAddress, long amount, long leftOverAmount)
		{
			TxOut txOut1 = new TxOut(receiverAddress, amount);

			if (leftOverAmount == 0)
			{
				return new List<TxOut> { txOut1 };
			}
			else
			{
				TxOut leftOverTx = new TxOut(myAddress, leftOverAmount);
				return new List<TxOut> { txOut1, leftOverTx };
			}
		}
	}
}