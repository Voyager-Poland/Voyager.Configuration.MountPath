using System;
using System.Text;

namespace Voyager.Configuration.MountPath.Encryption
{
	public class Encryptor
	{
		private readonly byte[] keyBytes;
		private readonly byte[] ivBytes;
		public Encryptor(string key)
		{
			keyBytes = Encoding.ASCII.GetBytes(key.Substring(0, 8));
			ivBytes = Encoding.ASCII.GetBytes(key.Substring(key.Length - 8, 8));
		}

		public string Encrypt(string dataParam)
		{
			using (LegacyDesCipherProvider coreEncoder = new LegacyDesCipherProvider(keyBytes, ivBytes))
			{
				return Convert.ToBase64String(coreEncoder.Encrypt(dataParam));
			}
		}

		public string Decrypt(string encryptedData)
		{
			using (LegacyDesCipherProvider coreEncoder = new LegacyDesCipherProvider(keyBytes, ivBytes))
				return coreEncoder.Decrypt(Convert.FromBase64String(encryptedData));
		}
	}
}
