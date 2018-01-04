namespace SimpleCoin.Node.Transactions
{
	using System.Text;
	using JetBrains.Annotations;
	using Org.BouncyCastle.Asn1.Sec;
	using Org.BouncyCastle.Asn1.X9;
	using Org.BouncyCastle.Crypto;
	using Org.BouncyCastle.Crypto.Parameters;
	using Org.BouncyCastle.Math;
	using Org.BouncyCastle.Math.EC;
	using Org.BouncyCastle.Security;
	using Org.BouncyCastle.Utilities.Encoders;

	[UsedImplicitly]
	public class Crypto
	{
		/// <summary>
		/// Creates a signature of the data to sign using the private key.
		/// </summary>
		/// <param name="dataToSign"></param>
		/// <param name="privateKey"></param>
		/// <returns></returns>
		public static string GetSignature(string dataToSign, string privateKey)
		{
			AsymmetricCipherKeyPair key = KeyFromPrivate(privateKey);

			byte[] inputData = Encoding.UTF8.GetBytes(dataToSign);

			ISigner signer = SignerUtilities.GetSigner("ECDSA");
			signer.Init(true, key.Private);
			signer.BlockUpdate(inputData, 0, inputData.Length);

			return Hex.ToHexString(signer.GenerateSignature());
		}

		/// <summary>
		/// Gets the public key from the private key.
		/// </summary>
		/// <param name="privateKey"></param>
		/// <returns></returns>
		public static string GetPublicKey(string privateKey)
		{
			BigInteger privateKeyInt = new BigInteger(privateKey, 16);

			X9ECParameters curve = SecNamedCurves.GetByName("secp256k1");
			ECDomainParameters domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

			ECPoint q = domain.G.Multiply(privateKeyInt);
			byte[] bytes = q.GetEncoded();

			return Hex.ToHexString(bytes);
		}

		/// <summary>
		/// Verifiies the signature of the data to sign against the given signature using the public key.
		/// </summary>
		/// <param name="publicKey"></param>
		/// <param name="dataToSign"></param>
		/// <param name="signature"></param>
		/// <returns></returns>
		public static bool VerifySignature(string publicKey, string dataToSign, string signature)
		{
			byte[] inputData = Encoding.UTF8.GetBytes(dataToSign);

			ECPublicKeyParameters publicKeyParam = KeyFromPublic(publicKey);

			ISigner signer = SignerUtilities.GetSigner("ECDSA");
			signer.Init(false, publicKeyParam);
			signer.BlockUpdate(inputData, 0, inputData.Length);

			return signer.VerifySignature(Hex.Decode(signature));
		}

		private static AsymmetricCipherKeyPair KeyFromPrivate(string privateKey)
		{
			BigInteger privateKeyInt = new BigInteger(privateKey, 16);

			X9ECParameters curve = SecNamedCurves.GetByName("secp256k1");
			ECDomainParameters domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

			ECPoint q = domain.G.Multiply(privateKeyInt);
			ECPrivateKeyParameters privateKeyParam = new ECPrivateKeyParameters(privateKeyInt, domain);
			ECPublicKeyParameters publicKeyParam = new ECPublicKeyParameters(q, domain);

			return new AsymmetricCipherKeyPair(publicKeyParam, privateKeyParam);
		}

		private static ECPublicKeyParameters KeyFromPublic(string publicKey)
		{
			BigInteger publicKeyInt = new BigInteger(publicKey, 16);

			X9ECParameters curve = SecNamedCurves.GetByName("secp256k1");
			ECDomainParameters domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());

			ECPublicKeyParameters key = new ECPublicKeyParameters("ECDSA", curve.Curve.DecodePoint(Hex.Decode(publicKey)), domain);
			return key;
		}
	}
}