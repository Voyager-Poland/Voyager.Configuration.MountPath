using System;

namespace Voyager.Configuration.MountPath.Encryption
{
	/// <summary>
	/// Defines the contract for low-level cipher operations.
	/// </summary>
	public interface ICipherProvider : IDisposable
	{
		/// <summary>
		/// Encrypts the specified plaintext string to bytes.
		/// </summary>
		/// <param name="plaintext">The plaintext to encrypt.</param>
		/// <returns>The encrypted data as a byte array.</returns>
		byte[] Encrypt(string plaintext);

		/// <summary>
		/// Decrypts the specified encrypted bytes to a string.
		/// </summary>
		/// <param name="encryptedData">The encrypted data as a byte array.</param>
		/// <returns>The decrypted plaintext.</returns>
		string Decrypt(byte[] encryptedData);
	}
}
