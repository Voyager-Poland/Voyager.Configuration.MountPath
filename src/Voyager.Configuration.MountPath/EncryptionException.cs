using System;

namespace Voyager.Configuration.MountPath
{
	/// <summary>
	/// Exception thrown when encryption or decryption fails.
	/// </summary>
	public class EncryptionException : ConfigurationException
	{
		/// <summary>
		/// Gets the configuration key that failed to encrypt/decrypt.
		/// </summary>
		public string? Key { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="EncryptionException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		public EncryptionException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EncryptionException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="innerException">The inner exception.</param>
		public EncryptionException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EncryptionException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="mountPath">The mount path where configuration was being loaded from.</param>
		/// <param name="fileName">The filename that was being loaded.</param>
		/// <param name="key">The configuration key that failed to encrypt/decrypt.</param>
		/// <param name="innerException">The inner exception.</param>
		public EncryptionException(
			string message,
			string? mountPath,
			string? fileName,
			string? key,
			Exception? innerException = null)
			: base(message, mountPath, fileName, innerException)
		{
			Key = key;
		}
	}
}
