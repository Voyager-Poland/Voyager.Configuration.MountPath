using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;

namespace Voyager.Configuration.MountPath.Encryption
{
	/// <summary>
	/// Configuration source for encrypted JSON files.
	/// </summary>
	public class EncryptedJsonConfigurationSource : JsonConfigurationSource
	{
		private string _key = string.Empty;

		/// <summary>
		/// Gets or sets the encryption key.
		/// </summary>
		/// <exception cref="ArgumentException">Thrown when key is null or whitespace.</exception>
		public string Key
		{
			get => _key;
			set
			{
				if (string.IsNullOrWhiteSpace(value))
					throw new ArgumentException("Encryption key cannot be null or whitespace.", nameof(Key));
				_key = value;
			}
		}

		/// <summary>
		/// Gets or sets the encryptor factory for creating encryptor instances.
		/// If not set, the default factory is used.
		/// </summary>
		public IEncryptorFactory? EncryptorFactory { get; set; }

		/// <inheritdoc />
		public override IConfigurationProvider Build(IConfigurationBuilder builder)
		{
			EnsureDefaults(builder);
			return new EncryptedJsonConfigurationProvider(this);
		}
	}
}
