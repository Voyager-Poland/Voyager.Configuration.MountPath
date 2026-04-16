using System;
using System.Security.Cryptography;
using System.Text;

namespace Voyager.Configuration.MountPath.Encryption
{
	/// <summary>
	/// Provides AES-256-GCM authenticated encryption per ADR-010.
	/// Each call produces a fresh 12-byte nonce; output layout is
	/// <c>nonce || ciphertext || tag</c> (tag is 16 bytes).
	/// </summary>
	/// <remarks>
	/// Requires a 32-byte key supplied as Base64. On .NET Framework 4.8 the
	/// constructor throws <see cref="PlatformNotSupportedException"/> — AES-GCM
	/// is not available in the BCL there.
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
		private bool _disposed;
#endif

		/// <summary>
		/// Initializes a new instance from a Base64-encoded 32-byte key.
		/// </summary>
		/// <param name="base64Key">Base64-encoded 256-bit key (e.g. from <c>vconfig keygen</c>).</param>
		/// <exception cref="ArgumentNullException">Key is <c>null</c>.</exception>
		/// <exception cref="EncryptionException">Key is not valid Base64 or is not 32 bytes.</exception>
		/// <exception cref="PlatformNotSupportedException">Running on .NET Framework where <c>AesGcm</c> is unavailable.</exception>
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
			throw new PlatformNotSupportedException(
				"AES-256-GCM is not supported on this target framework. " +
				"Use .NET Core 3.1 or later (or .NET 6+) to enable AES-GCM encryption.");
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

#if NETCOREAPP3_1_OR_GREATER || NET6_0_OR_GREATER
			ThrowIfDisposed();

			var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
			var nonce = new byte[NonceSizeBytes];
			RandomNumberGenerator.Fill(nonce);

			var ciphertext = new byte[plaintextBytes.Length];
			var tag = new byte[TagSizeBytes];

			_aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

			var output = new byte[NonceSizeBytes + ciphertext.Length + TagSizeBytes];
			Buffer.BlockCopy(nonce, 0, output, 0, NonceSizeBytes);
			Buffer.BlockCopy(ciphertext, 0, output, NonceSizeBytes, ciphertext.Length);
			Buffer.BlockCopy(tag, 0, output, NonceSizeBytes + ciphertext.Length, TagSizeBytes);
			return output;
#else
			throw new PlatformNotSupportedException("AES-256-GCM is not supported on this target framework.");
#endif
		}

		/// <inheritdoc />
		public string Decrypt(byte[] encryptedData)
		{
			if (encryptedData == null)
				throw new ArgumentNullException(nameof(encryptedData));

#if NETCOREAPP3_1_OR_GREATER || NET6_0_OR_GREATER
			ThrowIfDisposed();

			if (encryptedData.Length < NonceSizeBytes + TagSizeBytes)
			{
				throw new EncryptionException(
					$"Encrypted payload is too short ({encryptedData.Length} bytes). " +
					$"Expected at least {NonceSizeBytes + TagSizeBytes} bytes (nonce + tag).");
			}

			var ciphertextLength = encryptedData.Length - NonceSizeBytes - TagSizeBytes;

			var nonce = new byte[NonceSizeBytes];
			var ciphertext = new byte[ciphertextLength];
			var tag = new byte[TagSizeBytes];

			Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSizeBytes);
			Buffer.BlockCopy(encryptedData, NonceSizeBytes, ciphertext, 0, ciphertextLength);
			Buffer.BlockCopy(encryptedData, NonceSizeBytes + ciphertextLength, tag, 0, TagSizeBytes);

			var plaintextBytes = new byte[ciphertextLength];
			_aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);

			return Encoding.UTF8.GetString(plaintextBytes);
#else
			throw new PlatformNotSupportedException("AES-256-GCM is not supported on this target framework.");
#endif
		}

#if NETCOREAPP3_1_OR_GREATER || NET6_0_OR_GREATER
		private void ThrowIfDisposed()
		{
			if (_disposed)
				throw new ObjectDisposedException(nameof(AesGcmCipherProvider));
		}
#endif

		/// <inheritdoc />
		public void Dispose()
		{
#if NETCOREAPP3_1_OR_GREATER || NET6_0_OR_GREATER
			if (_disposed)
				return;
			_aesGcm?.Dispose();
			_disposed = true;
#endif
		}
	}
}
