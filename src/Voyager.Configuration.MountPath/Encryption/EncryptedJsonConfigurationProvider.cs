using Microsoft.Extensions.Configuration.Json;
using System.IO;

namespace Voyager.Configuration.MountPath.Encryption
{
	public class EncryptedJsonConfigurationProvider : JsonConfigurationProvider
	{
		Encryptor encryptor;

		public EncryptedJsonConfigurationProvider(EncryptedJsonConfigurationSource source) : base(source)
		{
			encryptor = new Encryptor(source.Key);
		}


		public override void Load(Stream stream)
		{
			base.Load(stream);

			foreach (string key in Data.Keys)
			{
				Data[key] = encryptor.Decrypt(Data[key]);
			}
		}
	}
}
