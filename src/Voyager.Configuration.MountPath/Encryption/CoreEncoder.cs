using System;
using System.IO;
using System.Security.Cryptography;

namespace Voyager.Configuration.MountPath.Encryption
{
	class CoreEncoder : IDisposable
	{
		private byte[] keyBytes;
		private byte[] ivBytes;
		private DESCryptoServiceProvider cryptoProvider;

		public CoreEncoder(byte[] keyBytes, byte[] ivBytes)
		{
			this.keyBytes = keyBytes;
			this.ivBytes = ivBytes;

			cryptoProvider = new DESCryptoServiceProvider();
		}

		public void Dispose()
		{
			cryptoProvider.Dispose();
		}

		public Byte[] Encrypt(string dataParam)
		{
			var transform = cryptoProvider.CreateEncryptor(keyBytes, ivBytes);
			using var ms = new MemoryStream();
			using var crypstream = new CryptoStream(ms, transform, CryptoStreamMode.Write);
			using var sw = new StreamWriter(crypstream);
			sw.Write(dataParam);
			sw.Close();
			crypstream.Close();
			return ms.ToArray();
		}

		public string Decrypt(byte[] dataParam)
		{
			using DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
			var result = string.Empty;
			using var ms = new MemoryStream(dataParam);
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
