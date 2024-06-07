using System;
using System.Text;

namespace Voyager.Configuration.MountPath.Encryption
{
	public class Encryptor
	{
		readonly byte[] keyBytes;
		readonly byte[] ivBytes;
		public Encryptor(string key)
		{
			keyBytes = Encoding.ASCII.GetBytes(key.Substring(0, 8));
			ivBytes = Encoding.ASCII.GetBytes(key.Substring(key.Length - 8, 8));
		}

		public string Encrypt(string dataParam)
		{
			using CoreEncoder coreEncoder = new CoreEncoder(keyBytes, ivBytes);
			return Convert.ToBase64String(coreEncoder.Encrypt(dataParam));
		}

		public string Decrypt(string dataParamtxt)
		{
			using CoreEncoder coreEncoder = new CoreEncoder(keyBytes, ivBytes);
			return coreEncoder.Decrypt(Convert.FromBase64String(dataParamtxt));
		}
	}
}
