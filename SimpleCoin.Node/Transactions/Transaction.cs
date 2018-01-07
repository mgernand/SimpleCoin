namespace SimpleCoin.Node.Transactions
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using Util;

	public sealed class Transaction : ICloneable, IEquatable<Transaction>
	{
		public const int CoinbaseAmount = 50;

		public Transaction()
		{
			this.TxIns = new List<TxIn>();
			this.TxOuts = new List<TxOut>();
		}

		/// <summary>
		/// The id of the transaction.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// The transaction inputs.
		/// </summary>
		public IList<TxIn> TxIns { get; set; }

		/// <summary>
		/// The transaction outputs.
		/// </summary>
		public IList<TxOut> TxOuts { get; set; }

		/// <summary>
		/// Calculates the tranaction id by creating a hash from the contents of the tranaction.
		/// The signatures of the TxIns are not included in the transaction hash, because they
		/// are added later on.
		/// </summary>
		/// <param name="transaction"></param>
		/// <returns></returns>
		public static string GetTransactionId(Transaction transaction)
		{
			string txInContent = transaction.TxIns
				.Select(x => x.TxOutId + x.TxOutIndex.ToString())
				.Aggregate((s1, s2) => s1 + s1);

			string txOutContent = transaction.TxOuts
				.Select(x => x.Address + x.Amount.ToString(CultureInfo.InvariantCulture))
				.Aggregate((s1, s2) => s1 + s2);

			return (txInContent + txOutContent).CalculateHash();
		}

		public static Transaction Genesis { get; } = new Transaction
		{
			TxIns = new List<TxIn> { new TxIn { Signature = "", TxOutId = "", TxOutIndex = 0 }},
			TxOuts = new List<TxOut> { new TxOut("04bfcab8722991ae774db48f934ca79cfb7dd991229153b9f732ba5334aafcd8e7266e47076996b55a14bf9913ee3145ce0cfc1372ada8ada74bd287450313534a", 50) },
			Id = "e655f6a5f26dc9b4cac6e46f52336428287759cf81ef5ff10854f69d68f43fa3"
		};


		public static Transaction GetCoinbaseTransaction(string address, int blockIndex)
		{
			Transaction transaction = new Transaction();
			TxIn txIn = new TxIn
			{
				Signature = "",
				TxOutId = "",
				TxOutIndex = blockIndex,
			};

			transaction.TxIns.Add(txIn);
			transaction.TxOuts.Add(new TxOut(address, CoinbaseAmount));
			transaction.Id = GetTransactionId(transaction);

			return transaction;
		}

		/// <inheritdoc />
		public object Clone()
		{
			return new Transaction
			{
				TxIns = this.TxIns.Select(tx => (TxIn)tx.Clone()).ToList(),
				TxOuts = this.TxOuts.Select(tx => (TxOut)tx.Clone()).ToList(),
				Id = this.Id,
			};
		}

		/// <inheritdoc />
		public bool Equals(Transaction other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return string.Equals(this.Id, other.Id, StringComparison.InvariantCulture);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is Transaction && this.Equals((Transaction) obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return (this.Id != null ? StringComparer.InvariantCulture.GetHashCode(this.Id) : 0);
		}

		public static bool operator ==(Transaction left, Transaction right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(Transaction left, Transaction right)
		{
			return !Equals(left, right);
		}
	}
}