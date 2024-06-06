using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Voyager.Configuration.MountPath.Encryption
{
	internal class Encryptor
	{
		byte[] keyBytes;
		byte[] ivBytes;
		public Encryptor(string key)
		{
			keyBytes = Encoding.ASCII.GetBytes(key.Substring(0, 8));
			ivBytes = Encoding.ASCII.GetBytes(key.Substring(key.Length - 8, 8));
		}

		public string Encrypt(string dataParam)
		{
			using DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
			var transform = cryptoProvider.CreateEncryptor(keyBytes, ivBytes);
			using var ms = new MemoryStream();
			using var crypstream = new CryptoStream(ms, transform, CryptoStreamMode.Write);
			using var sw = new StreamWriter(crypstream);
			sw.Write(dataParam);
			sw.Close();
			crypstream.Close();
			return Convert.ToBase64String(ms.ToArray());
		}


		public string Decrypt(string dataParamtxt)
		{
			using DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
			var result = string.Empty;
			using var ms = new MemoryStream(Convert.FromBase64String(dataParamtxt));
			using (var crypstream = new CryptoStream(ms, cryptoProvider.CreateDecryptor(keyBytes, ivBytes), CryptoStreamMode.Read))
			{
				using (var sr = new StreamReader(crypstream))
				{
					result = sr.ReadToEnd();
					sr.Close();
				}
				crypstream.Close();
			}
			ms.Close();
			return result;
		}
	}
}
