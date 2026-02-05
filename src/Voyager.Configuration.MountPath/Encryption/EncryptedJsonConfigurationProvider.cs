using Microsoft.Extensions.Configuration.Json;
using System;
using System.IO;

namespace Voyager.Configuration.MountPath.Encryption
{
	/// <summary>
	/// Configuration provider that decrypts encrypted JSON configuration values.
	/// </summary>
	public class EncryptedJsonConfigurationProvider : JsonConfigurationProvider
	{
		private readonly IEncryptor encryptor;

		/// <summary>
		/// Initializes a new instance of the <see cref="EncryptedJsonConfigurationProvider"/> class.
		/// </summary>
		/// <param name="source">The configuration source.</param>
		/// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when no key is provided.</exception>
		public EncryptedJsonConfigurationProvider(EncryptedJsonConfigurationSource source) : base(source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (string.IsNullOrEmpty(source.Key))
				throw new InvalidOperationException("Encryption key must be provided.");

			var factory = source.EncryptorFactory ?? new DefaultEncryptorFactory();
			encryptor = factory.Create(source.Key);
		}

		/// <inheritdoc />
		public override void Load(Stream stream)
		{
			base.Load(stream);

			foreach (string key in Data.Keys)
			{
				Data[key] = encryptor.Decrypt(Data[key]);
			}
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (encryptor is IDisposable disposableEncryptor)
				{
					disposableEncryptor.Dispose();
				}
			}
			base.Dispose(disposing);
		}
	}
}
