using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Voyager.Configuration.MountPath.Encryption;

namespace Voyager.Configuration.MountPath.Test
{
	/// <summary>
	/// Tests for encryption key validation edge cases.
	/// </summary>
	[TestFixture]
	public class KeyValidationTest
	{
		[Test]
		public void EncryptedJsonConfigurationSource_WithNullKey_ThrowsArgumentException()
		{
			var source = new EncryptedJsonConfigurationSource
			{
				Path = "test.json"
			};

			Assert.Throws<ArgumentException>(() => source.Key = null!);
		}

		[Test]
		public void EncryptedJsonConfigurationSource_WithEmptyKey_ThrowsArgumentException()
		{
			var source = new EncryptedJsonConfigurationSource
			{
				Path = "test.json"
			};

			Assert.Throws<ArgumentException>(() => source.Key = string.Empty);
		}

		[Test]
		public void EncryptedJsonConfigurationSource_WithWhitespaceKey_ThrowsArgumentException()
		{
			var source = new EncryptedJsonConfigurationSource
			{
				Path = "test.json"
			};

			Assert.Throws<ArgumentException>(() => source.Key = "   ");
		}

		[Test]
		public void EncryptedJsonConfigurationProvider_WithNullSource_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new EncryptedJsonConfigurationProvider(null!));
		}

		[Test]
		public void EncryptedJsonConfigurationProvider_WithEmptyKeyInSource_ThrowsInvalidOperationException()
		{
			var source = new EncryptedJsonConfigurationSource
			{
				Path = "test.json",
				FileProvider = new NullFileProvider()
			};

			// Reset key to empty by accessing private field (for testing edge case)
			var ex = Assert.Throws<InvalidOperationException>(() =>
			{
				var provider = new EncryptedJsonConfigurationProvider(source);
			});

			Assert.That(ex.Message, Does.Contain("Encryption key must be provided"));
		}

		[Test]
		public void EncryptedJsonConfigurationSource_WithValidKey_DoesNotThrow()
		{
			var source = new EncryptedJsonConfigurationSource
			{
				Path = "test.json",
				FileProvider = new NullFileProvider()
			};

			Assert.DoesNotThrow(() => source.Key = "ValidKey123");
			Assert.That(source.Key, Is.EqualTo("ValidKey123"));
		}

		[Test]
		public void EncryptedJsonConfigurationSource_KeyIsPreserved()
		{
			var source = new EncryptedJsonConfigurationSource
			{
				Path = "test.json",
				Key = "MySecretKey123"
			};

			Assert.That(source.Key, Is.EqualTo("MySecretKey123"));
		}

		[Test]
		public void EncryptedJsonConfigurationSource_CanSetKeyMultipleTimes()
		{
			var source = new EncryptedJsonConfigurationSource
			{
				Path = "test.json"
			};

			source.Key = "FirstKey";
			Assert.That(source.Key, Is.EqualTo("FirstKey"));

			source.Key = "SecondKey";
			Assert.That(source.Key, Is.EqualTo("SecondKey"));
		}

		[Test]
		public void EncryptorFactory_CanBeSet()
		{
			var source = new EncryptedJsonConfigurationSource
			{
				Path = "test.json",
				Key = "TestKey"
			};

			var factory = new DefaultEncryptorFactory();
			Assert.DoesNotThrow(() => source.EncryptorFactory = factory);
			Assert.That(source.EncryptorFactory, Is.SameAs(factory));
		}

		[Test]
		public void EncryptorFactory_CanBeNull()
		{
			var source = new EncryptedJsonConfigurationSource
			{
				Path = "test.json",
				Key = "TestKey",
				EncryptorFactory = null
			};

			Assert.That(source.EncryptorFactory, Is.Null);
		}

		[Test]
		public void Encryptor_WithNullKey_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new Encryptor(null!));
		}

		[Test]
		public void Encryptor_WithEmptyKey_ThrowsArgumentException()
		{
			Assert.Throws<ArgumentException>(() => new Encryptor(string.Empty));
		}

		[Test]
		public void Encryptor_WithWhitespaceKey_ThrowsArgumentException()
		{
			Assert.Throws<ArgumentException>(() => new Encryptor("   "));
		}

		[Test]
		public void Encryptor_WithShortKey_ThrowsArgumentException()
		{
			// DES encryption requires minimum key length
			Assert.Throws<ArgumentException>(() => new Encryptor("short"));
		}

		[Test]
		public void Encryptor_WithValidKey_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => new Encryptor("ValidKey1234567890"));
		}

		[Test]
		public void Encryptor_EncryptDecrypt_RoundTrip()
		{
			var encryptor = new Encryptor("MySecretKey1234567890");
			var original = "Test Value 123";

			var encrypted = encryptor.Encrypt(original);
			var decrypted = encryptor.Decrypt(encrypted);

			Assert.That(decrypted, Is.EqualTo(original));
			Assert.That(encrypted, Is.Not.EqualTo(original));
		}

		[Test]
		public void Encryptor_DecryptWithNullValue_ThrowsArgumentNullException()
		{
			var encryptor = new Encryptor("MySecretKey1234567890");

			Assert.Throws<ArgumentNullException>(() => encryptor.Decrypt(null!));
		}

		[Test]
		public void Encryptor_EncryptWithNullValue_ThrowsArgumentNullException()
		{
			var encryptor = new Encryptor("MySecretKey1234567890");

			Assert.Throws<ArgumentNullException>(() => encryptor.Encrypt(null!));
		}

		[Test]
		public void Encryptor_DecryptInvalidBase64_ThrowsFormatException()
		{
			var encryptor = new Encryptor("MySecretKey1234567890");

			Assert.Throws<FormatException>(() => encryptor.Decrypt("not-valid-base64!@#$"));
		}

		[Test]
		public void Encryptor_DecryptWithWrongKey_ThrowsCryptographicException()
		{
			var encryptor1 = new Encryptor("Key1234567890123456");
			var encryptor2 = new Encryptor("DifferentKey12345678");

			var encrypted = encryptor1.Encrypt("Test Value");

			Assert.Throws<System.Security.Cryptography.CryptographicException>(() => encryptor2.Decrypt(encrypted));
		}
	}
}
