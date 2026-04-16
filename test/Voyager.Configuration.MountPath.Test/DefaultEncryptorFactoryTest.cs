using System.Security.Cryptography;
using Voyager.Configuration.MountPath.Encryption;

namespace Voyager.Configuration.MountPath.Test
{
	[TestFixture]
	public class DefaultEncryptorFactoryTest
	{
		private static string GenerateBase64AesKey()
		{
			return Convert.ToBase64String(RandomNumberGenerator.GetBytes(AesGcmCipherProvider.KeySizeBytes));
		}

		[Test]
		public void Create_Base64AesKey_ReturnsVersionedEncryptor()
		{
			var factory = new DefaultEncryptorFactory();

			var encryptor = factory.Create(GenerateBase64AesKey());

			Assert.That(encryptor, Is.InstanceOf<VersionedEncryptor>());
		}

		[Test]
		public void Create_Base64AesKey_EncryptUsesV2Prefix()
		{
			var factory = new DefaultEncryptorFactory();
			var encryptor = factory.Create(GenerateBase64AesKey());

			var ciphertext = encryptor.Encrypt("payload");

			Assert.That(ciphertext, Does.StartWith(VersionedEncryptor.V2Prefix));
		}

		[Test]
		public void Create_LegacyShortKey_ReturnsLegacyEncryptor()
		{
			var factory = new DefaultEncryptorFactory();

			var encryptor = factory.Create("LegacyDesKey1234");

			Assert.That(encryptor, Is.InstanceOf<Encryptor>());
		}

		[Test]
		public void Create_NullKey_ThrowsArgumentNullException()
		{
			var factory = new DefaultEncryptorFactory();

			Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
		}

		[Test]
		public void Create_UnusableKey_ThrowsEncryptionException()
		{
			var factory = new DefaultEncryptorFactory();

			Assert.Throws<EncryptionException>(() => factory.Create("a"));
		}

		[Test]
		public void Create_WithAesKey_ReadsLegacyDesCiphertext_WhenAllowLegacyDesTrue()
		{
			var factory = new DefaultEncryptorFactory();
			var aesKey = GenerateBase64AesKey();
			var legacyCiphertext = new Encryptor(aesKey).Encrypt("legacy payload");

			var encryptor = factory.Create(aesKey);
			var decrypted = encryptor.Decrypt(legacyCiphertext);

			Assert.That(decrypted, Is.EqualTo("legacy payload"));
		}

		[Test]
		public void Create_AllowLegacyDesFalse_RejectsLegacyDesCiphertext()
		{
			var factory = new DefaultEncryptorFactory { AllowLegacyDes = false };
			var aesKey = GenerateBase64AesKey();
			var legacyCiphertext = new Encryptor(aesKey).Encrypt("legacy payload");

			var encryptor = factory.Create(aesKey);

			Assert.Throws<EncryptionException>(() => encryptor.Decrypt(legacyCiphertext));
		}
	}
}
