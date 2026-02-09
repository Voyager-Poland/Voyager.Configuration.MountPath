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
	/// Default factory that creates <see cref="Encryptor"/> instances.
	/// </summary>
	public class DefaultEncryptorFactory : IEncryptorFactory
	{
		/// <inheritdoc />
		public IEncryptor Create(string key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			return new Encryptor(key);
		}
	}
}
