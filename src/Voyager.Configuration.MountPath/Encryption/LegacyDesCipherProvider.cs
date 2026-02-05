using System;
using System.IO;
using System.Security.Cryptography;

namespace Voyager.Configuration.MountPath.Encryption
{
	/// <summary>
	/// Provides legacy DES encryption/decryption.
	/// This class is deprecated and should only be used for backward compatibility.
	/// </summary>
	[Obsolete("DES encryption is deprecated. Use AES-256-GCM for new implementations.")]
	internal class LegacyDesCipherProvider : ICipherProvider
	{
		private readonly byte[] keyBytes;
		private readonly byte[] ivBytes;
		private readonly DESCryptoServiceProvider cryptoProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="LegacyDesCipherProvider"/> class.
		/// </summary>
		/// <param name="keyBytes">The 8-byte encryption key.</param>
		/// <param name="ivBytes">The 8-byte initialization vector.</param>
		public LegacyDesCipherProvider(byte[] keyBytes, byte[] ivBytes)
		{
			this.keyBytes = keyBytes ?? throw new ArgumentNullException(nameof(keyBytes));
			this.ivBytes = ivBytes ?? throw new ArgumentNullException(nameof(ivBytes));

			cryptoProvider = new DESCryptoServiceProvider();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			cryptoProvider.Dispose();
		}

		/// <inheritdoc />
		public byte[] Encrypt(string plaintext)
		{
			if (plaintext == null)
				throw new ArgumentNullException(nameof(plaintext));

			var transform = cryptoProvider.CreateEncryptor(keyBytes, ivBytes);
			using (var ms = new MemoryStream())
			using (var cryptoStream = new CryptoStream(ms, transform, CryptoStreamMode.Write))
			using (var writer = new StreamWriter(cryptoStream))
			{
				writer.Write(plaintext);
				writer.Flush();
				cryptoStream.FlushFinalBlock();
				return ms.ToArray();
			}
		}

		/// <inheritdoc />
		public string Decrypt(byte[] encryptedData)
		{
			if (encryptedData == null)
				throw new ArgumentNullException(nameof(encryptedData));

			using (var cryptoProvider = new DESCryptoServiceProvider())
			using (var ms = new MemoryStream(encryptedData))
			using (var cryptoStream = new CryptoStream(ms, cryptoProvider.CreateDecryptor(keyBytes, ivBytes), CryptoStreamMode.Read))
			using (var reader = new StreamReader(cryptoStream))
			{
				return reader.ReadToEnd();
			}
		}
	}
}
