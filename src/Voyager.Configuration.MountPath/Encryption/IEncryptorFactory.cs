using System;

namespace Voyager.Configuration.MountPath.Encryption
{
	/// <summary>
	/// Factory interface for creating <see cref="IEncryptor"/> instances.
	/// </summary>
	public interface IEncryptorFactory
	{
		/// <summary>
		/// Creates an encryptor with the specified key.
		/// </summary>
		/// <param name="key">The encryption key.</param>
		/// <returns>An instance of <see cref="IEncryptor"/>.</returns>
		IEncryptor Create(string key);
	}

	/// <summary>
	/// Default factory. Produces a <see cref="VersionedEncryptor"/> when the key
	/// decodes to a 32-byte AES-256 key (per ADR-010); falls back to a legacy
	/// DES <see cref="Encryptor"/> when the key is the older short-string form.
	/// </summary>
	public class DefaultEncryptorFactory : IEncryptorFactory
	{
		/// <summary>
		/// When <c>true</c>, unversioned (legacy DES) ciphertexts are decrypted
		/// for backward compatibility. Default <c>true</c> in v2.x, planned to
		/// flip to <c>false</c> in v3.x and be removed in v4.x.
		/// </summary>
		public bool AllowLegacyDes { get; set; } = true;

		/// <summary>Optional sink for the one-shot legacy-DES migration warning.</summary>
		public Action<string>? WarningLogger { get; set; }

		/// <inheritdoc />
		public IEncryptor Create(string key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			var aes = TryCreateAes(key);
			var legacyDes = TryCreateLegacyDes(key);

			if (aes == null && legacyDes == null)
				throw new EncryptionException(
					"Encryption key is invalid: neither a Base64-encoded 32-byte AES key " +
					"nor a legacy DES key (>= 8 chars). Generate a fresh key with `vconfig keygen`.");

			if (aes == null)
				return legacyDes!;

			return new VersionedEncryptor(aes, legacyDes, AllowLegacyDes, WarningLogger);
		}

		private static AesGcmCipherProvider? TryCreateAes(string key)
		{
			try
			{
				return new AesGcmCipherProvider(key);
			}
			catch (EncryptionException)
			{
				return null;
			}
			catch (PlatformNotSupportedException)
			{
				return null;
			}
		}

		private static IEncryptor? TryCreateLegacyDes(string key)
		{
			try
			{
				return new Encryptor(key);
			}
			catch (ArgumentException)
			{
				return null;
			}
		}
	}
}
