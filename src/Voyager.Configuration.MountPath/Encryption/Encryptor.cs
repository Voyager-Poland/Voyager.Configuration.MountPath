using System;
using System.Text;

namespace Voyager.Configuration.MountPath.Encryption
{
	/// <summary>
	/// Provides encryption and decryption services using a symmetric key.
	/// </summary>
	public class Encryptor : IEncryptor
	{
		private readonly byte[] keyBytes;
		private readonly byte[] ivBytes;

		/// <summary>
		/// Initializes a new instance of the <see cref="Encryptor"/> class.
		/// </summary>
		/// <param name="key">The encryption key. Must be at least 8 characters long.</param>
		/// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
		/// <exception cref="ArgumentException">Thrown when key is too short.</exception>
		public Encryptor(string key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			if (key.Length < 8)
				throw new ArgumentException("Key must be at least 8 characters long.", nameof(key));

			keyBytes = Encoding.ASCII.GetBytes(key.Substring(0, 8));
			ivBytes = Encoding.ASCII.GetBytes(key.Substring(key.Length - 8, 8));
		}

		/// <inheritdoc />
		public string Encrypt(string plaintext)
		{
			if (plaintext == null)
				throw new ArgumentNullException(nameof(plaintext));

#pragma warning disable CS0618 // LegacyDesCipherProvider is intentionally used for backward compatibility
			using (var cipherProvider = new LegacyDesCipherProvider(keyBytes, ivBytes))
#pragma warning restore CS0618
			{
				return Convert.ToBase64String(cipherProvider.Encrypt(plaintext));
			}
		}

		/// <inheritdoc />
		public string Decrypt(string encryptedData)
		{
			if (encryptedData == null)
				throw new ArgumentNullException(nameof(encryptedData));

#pragma warning disable CS0618 // LegacyDesCipherProvider is intentionally used for backward compatibility
			using (var cipherProvider = new LegacyDesCipherProvider(keyBytes, ivBytes))
#pragma warning restore CS0618
			{
				return cipherProvider.Decrypt(Convert.FromBase64String(encryptedData));
			}
		}
	}
}
