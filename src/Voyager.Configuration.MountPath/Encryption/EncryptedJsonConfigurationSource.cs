using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Voyager.Configuration.MountPath.Encryption
{
	/// <summary>
	/// Configuration source for encrypted JSON files.
	/// </summary>
	public class EncryptedJsonConfigurationSource : JsonConfigurationSource
	{
		/// <summary>
		/// Gets or sets the encryption key.
		/// </summary>
		public string Key { get; set; } = string.Empty;

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
