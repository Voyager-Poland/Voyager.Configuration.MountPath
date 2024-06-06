using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.IO;

namespace Voyager.Configuration.MountPath.Encryption
{
	public class EncryptedJsonConfigurationSource : JsonConfigurationSource
	{

		public string Key { get; set; } = "DEFAULT123456789011";
		public override IConfigurationProvider Build(IConfigurationBuilder builder)
		{
			EnsureDefaults(builder);
			return new EncryptedJsonConfigurationProvider(this);
		}

	}
}
