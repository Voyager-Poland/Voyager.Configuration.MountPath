using System;
using System.Security.Cryptography;
using System.Text;
#if NET48
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
#endif

namespace Voyager.Configuration.MountPath.Encryption
{
	/// <summary>
	/// Provides AES-256-GCM authenticated encryption per ADR-010.
	/// Each call produces a fresh 12-byte nonce; output layout is
	/// <c>nonce || ciphertext || tag</c> (tag is 16 bytes).
	/// </summary>
	/// <remarks>
	/// Requires a 32-byte key supplied as Base64. On .NET Core 3.1+ / .NET 6+
	/// delegates to BCL <c>System.Security.Cryptography.AesGcm</c>; on
	/// .NET Framework 4.8 delegates to BouncyCastle <c>GcmBlockCipher</c>.
	/// Wire format is identical byte-for-byte across backends, so ciphertexts
	/// are portable between TFMs.
	/// </remarks>
	public sealed class AesGcmCipherProvider : ICipherProvider
	{
		/// <summary>AES-256 key length in bytes (32).</summary>
		public const int KeySizeBytes = 32;
		/// <summary>Nonce length in bytes (12) — per NIST SP 800-38D recommendation for AES-GCM.</summary>
		public const int NonceSizeBytes = 12;
		/// <summary>Authentication tag length in bytes (16).</summary>
		public const int TagSizeBytes = 16;

#if NETCOREAPP3_1_OR_GREATER || NET6_0_OR_GREATER
		private readonly AesGcm _aesGcm;
#else
		private readonly byte[] _key;
#endif
		private bool _disposed;

		/// <summary>
		/// Initializes a new instance from a Base64-encoded 32-byte key.
		/// </summary>
		/// <param name="base64Key">Base64-encoded 256-bit key (e.g. from <c>vconfig keygen</c>).</param>
		/// <exception cref="ArgumentNullException">Key is <c>null</c>.</exception>
		/// <exception cref="EncryptionException">Key is not valid Base64 or is not 32 bytes.</exception>
		public AesGcmCipherProvider(string base64Key)
		{
			if (base64Key == null)
				throw new ArgumentNullException(nameof(base64Key));

			var keyBytes = DecodeKey(base64Key);

#if NET8_0_OR_GREATER
			_aesGcm = new AesGcm(keyBytes, TagSizeBytes);
#elif NETCOREAPP3_1_OR_GREATER || NET6_0_OR_GREATER
			_aesGcm = new AesGcm(keyBytes);
#else
			_key = keyBytes;
#endif
		}

		private static byte[] DecodeKey(string base64Key)
		{
			byte[] keyBytes;
			try
			{
				keyBytes = Convert.FromBase64String(base64Key);
			}
			catch (FormatException ex)
			{
				throw new EncryptionException(
					"Encryption key is not valid Base64. " +
					"Generate a fresh key with `vconfig keygen` and set it in ASPNETCORE_ENCODEKEY.",
					ex);
			}

			if (keyBytes.Length != KeySizeBytes)
			{
				throw new EncryptionException(
					$"Encryption key must decode to exactly {KeySizeBytes} bytes (got {keyBytes.Length}). " +
					"Generate a fresh key with `vconfig keygen` and set it in ASPNETCORE_ENCODEKEY.");
			}

			return keyBytes;
		}

		/// <inheritdoc />
		public byte[] Encrypt(string plaintext)
		{
			if (plaintext == null)
				throw new ArgumentNullException(nameof(plaintext));
			ThrowIfDisposed();

			var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
			var nonce = GenerateNonce();
			var output = new byte[NonceSizeBytes + plaintextBytes.Length + TagSizeBytes];
			Buffer.BlockCopy(nonce, 0, output, 0, NonceSizeBytes);

#if NETCOREAPP3_1_OR_GREATER || NET6_0_OR_GREATER
			var ciphertext = new byte[plaintextBytes.Length];
			var tag = new byte[TagSizeBytes];
			_aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
			Buffer.BlockCopy(ciphertext, 0, output, NonceSizeBytes, ciphertext.Length);
			Buffer.BlockCopy(tag, 0, output, NonceSizeBytes + ciphertext.Length, TagSizeBytes);
#else
			var cipher = CreateBouncyCastleCipher(forEncryption: true, nonce);
			var buffer = new byte[cipher.GetOutputSize(plaintextBytes.Length)];
			var written = cipher.ProcessBytes(plaintextBytes, 0, plaintextBytes.Length, buffer, 0);
			cipher.DoFinal(buffer, written);
			Buffer.BlockCopy(buffer, 0, output, NonceSizeBytes, buffer.Length);
#endif
			return output;
		}

		/// <inheritdoc />
		public string Decrypt(byte[] encryptedData)
		{
			if (encryptedData == null)
				throw new ArgumentNullException(nameof(encryptedData));
			ThrowIfDisposed();

			if (encryptedData.Length < NonceSizeBytes + TagSizeBytes)
			{
				throw new EncryptionException(
					$"Encrypted payload is too short ({encryptedData.Length} bytes). " +
					$"Expected at least {NonceSizeBytes + TagSizeBytes} bytes (nonce + tag).");
			}

			var ciphertextLength = encryptedData.Length - NonceSizeBytes - TagSizeBytes;
			var nonce = new byte[NonceSizeBytes];
			Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSizeBytes);

#if NETCOREAPP3_1_OR_GREATER || NET6_0_OR_GREATER
			var ciphertext = new byte[ciphertextLength];
			var tag = new byte[TagSizeBytes];
			Buffer.BlockCopy(encryptedData, NonceSizeBytes, ciphertext, 0, ciphertextLength);
			Buffer.BlockCopy(encryptedData, NonceSizeBytes + ciphertextLength, tag, 0, TagSizeBytes);

			var plaintextBytes = new byte[ciphertextLength];
			_aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);
			return Encoding.UTF8.GetString(plaintextBytes);
#else
			var input = new byte[ciphertextLength + TagSizeBytes];
			Buffer.BlockCopy(encryptedData, NonceSizeBytes, input, 0, input.Length);

			var cipher = CreateBouncyCastleCipher(forEncryption: false, nonce);
			var buffer = new byte[cipher.GetOutputSize(input.Length)];
			int written;
			try
			{
				written = cipher.ProcessBytes(input, 0, input.Length, buffer, 0);
				written += cipher.DoFinal(buffer, written);
			}
			catch (InvalidCipherTextException ex)
			{
				// Wrong key or tampered payload — surface as the standard BCL exception type
				// so callers can uniformly catch CryptographicException regardless of backend.
				throw new CryptographicException("The computed authentication tag did not match the input authentication tag.", ex);
			}

			return Encoding.UTF8.GetString(buffer, 0, written);
#endif
		}

		private static byte[] GenerateNonce()
		{
			var nonce = new byte[NonceSizeBytes];
#if NETCOREAPP3_1_OR_GREATER || NET6_0_OR_GREATER
			RandomNumberGenerator.Fill(nonce);
#else
			using (var rng = RandomNumberGenerator.Create())
				rng.GetBytes(nonce);
#endif
			return nonce;
		}

#if NET48
		private GcmBlockCipher CreateBouncyCastleCipher(bool forEncryption, byte[] nonce)
		{
			var cipher = new GcmBlockCipher(new AesEngine());
			cipher.Init(forEncryption, new AeadParameters(new KeyParameter(_key), TagSizeBytes * 8, nonce));
			return cipher;
		}
#endif

		private void ThrowIfDisposed()
		{
			if (_disposed)
				throw new ObjectDisposedException(nameof(AesGcmCipherProvider));
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (_disposed)
				return;
#if NETCOREAPP3_1_OR_GREATER || NET6_0_OR_GREATER
			_aesGcm?.Dispose();
#else
			Array.Clear(_key, 0, _key.Length);
#endif
			_disposed = true;
		}
	}
}
