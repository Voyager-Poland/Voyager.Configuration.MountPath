namespace Voyager.Configuration.MountPath.Encryption
{
	/// <summary>
	/// Defines the contract for encrypting and decrypting string data.
	/// </summary>
	public interface IEncryptor
	{
		/// <summary>
		/// Encrypts the specified plaintext string.
		/// </summary>
		/// <param name="plaintext">The plaintext to encrypt.</param>
		/// <returns>The encrypted data as a base64-encoded string.</returns>
		string Encrypt(string plaintext);

		/// <summary>
		/// Decrypts the specified encrypted string.
		/// </summary>
		/// <param name="encryptedData">The encrypted data as a base64-encoded string.</param>
		/// <returns>The decrypted plaintext.</returns>
		string Decrypt(string encryptedData);
	}
}
