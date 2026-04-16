using System;
using System.Diagnostics;
using System.Threading;

namespace Voyager.Configuration.MountPath.Encryption
{
	/// <summary>
	/// Routes decryption to AES-256-GCM or legacy DES based on the versioned
	/// prefix defined in ADR-010. Writes always emit AES in the form
	/// <c>v2:BASE64(nonce || ciphertext || tag)</c>.
	/// </summary>
	/// <remarks>
	/// Dispatch is deterministic — a value starting with <c>v2:</c> is decrypted
	/// with AES; anything else is treated as legacy DES (Base64, no prefix).
	/// No try/catch between algorithms: DES-CBC can silently return garbage for
	/// non-DES inputs, so the version prefix is the only safe discriminator.
	/// </remarks>
	public sealed class VersionedEncryptor : IEncryptor, IDisposable
	{
		/// <summary>Wire-format version prefix marking AES-256-GCM ciphertexts.</summary>
		public const string V2Prefix = "v2:";

		private readonly AesGcmCipherProvider? _aes;
		private readonly IEncryptor? _legacyDes;
		private readonly bool _allowLegacyDes;
		private readonly Action<string>? _warningLogger;
		private int _legacyWarningEmitted;

		/// <summary>
		/// Initializes a new instance of the <see cref="VersionedEncryptor"/> class.
		/// </summary>
		/// <param name="aes">AES-256-GCM cipher provider used for writes and <c>v2:</c> reads. May be null when running on a platform that lacks AES-GCM; in that case writes fail.</param>
		/// <param name="legacyDes">Legacy DES encryptor used for unversioned reads. May be null when legacy support is not required.</param>
		/// <param name="allowLegacyDes">If <c>true</c>, unversioned values are decrypted with <paramref name="legacyDes"/>. Default is <c>true</c> (v2.x behavior).</param>
		/// <param name="warningLogger">Invoked once with a migration hint the first time a legacy DES value is read.</param>
		public VersionedEncryptor(
			AesGcmCipherProvider? aes,
			IEncryptor? legacyDes,
			bool allowLegacyDes = true,
			Action<string>? warningLogger = null)
		{
			_aes = aes;
			_legacyDes = legacyDes;
			_allowLegacyDes = allowLegacyDes;
			_warningLogger = warningLogger;
		}

		/// <inheritdoc />
		public string Encrypt(string plaintext)
		{
			if (plaintext == null)
				throw new ArgumentNullException(nameof(plaintext));
			if (_aes == null)
				throw new EncryptionException(
					"AES-256-GCM encryptor is not configured. " +
					"Provide a Base64-encoded 32-byte key (generate with `vconfig keygen`).");

			var bytes = _aes.Encrypt(plaintext);
			return V2Prefix + Convert.ToBase64String(bytes);
		}

		/// <inheritdoc />
		public string Decrypt(string encryptedData)
		{
			if (encryptedData == null)
				throw new ArgumentNullException(nameof(encryptedData));

			if (encryptedData.StartsWith(V2Prefix, StringComparison.Ordinal))
			{
				if (_aes == null)
					throw new EncryptionException(
						"Encountered a `v2:` (AES-256-GCM) ciphertext but the AES cipher is not configured. " +
						"Provide a Base64-encoded 32-byte key (generate with `vconfig keygen`).");

				var payload = encryptedData.Substring(V2Prefix.Length);
				byte[] bytes;
				try
				{
					bytes = Convert.FromBase64String(payload);
				}
				catch (FormatException ex)
				{
					throw new EncryptionException(
						"Malformed `v2:` ciphertext — payload is not valid Base64.", ex);
				}

				return _aes.Decrypt(bytes);
			}

			if (!_allowLegacyDes)
				throw new EncryptionException(
					"Legacy DES ciphertext detected but AllowLegacyDes is disabled. " +
					"Re-encrypt the file with `vconfig reencrypt` or enable AllowLegacyDes.");

			if (_legacyDes == null)
				throw new EncryptionException(
					"Legacy DES ciphertext detected but no legacy DES encryptor is configured.");

			EmitLegacyWarningOnce();
			return _legacyDes.Decrypt(encryptedData);
		}

		private void EmitLegacyWarningOnce()
		{
			// Atomically flip 0→1 exactly once. Concurrent Decrypt callers lose the race
			// and return without emitting a duplicate warning.
			if (Interlocked.CompareExchange(ref _legacyWarningEmitted, 1, 0) != 0)
				return;

			const string message =
				"Decrypting legacy DES-encrypted configuration value. " +
				"DES is obsolete — run `vconfig reencrypt` to migrate to AES-256-GCM.";

			if (_warningLogger != null)
			{
				_warningLogger(message);
			}
			else
			{
				Trace.TraceWarning(message);
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_aes?.Dispose();
			if (_legacyDes is IDisposable disposableLegacy)
				disposableLegacy.Dispose();
		}
	}
}
