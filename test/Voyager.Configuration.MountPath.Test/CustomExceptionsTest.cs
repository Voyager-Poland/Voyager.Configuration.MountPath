using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Voyager.Configuration.MountPath.Encryption;

namespace Voyager.Configuration.MountPath.Test
{
	[TestFixture]
	public class CustomExceptionsTest
	{
		private string _testDirectory;
		private string _testFilePath;

		[SetUp]
		public void SetUp()
		{
			_testDirectory = Path.Combine(Path.GetTempPath(), "VoyagerConfigTest_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_testDirectory);
			_testFilePath = Path.Combine(_testDirectory, "test.json");
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(_testDirectory))
			{
				Directory.Delete(_testDirectory, true);
			}
		}

		[Test]
		public void Load_WhenFileNotFound_ThrowsConfigurationException()
		{
			// Arrange
			var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");
			var source = new EncryptedJsonConfigurationSource
			{
				Path = nonExistentPath,
				Key = "TestKey123456",
				Optional = false
			};
			source.ResolveFileProvider();
			var provider = new EncryptedJsonConfigurationProvider(source);

			// Act & Assert
			var ex = Assert.Throws<ConfigurationException>(() => provider.Load());
			Assert.That(ex.Message, Does.Contain("File not found"));
			Assert.That(ex.Message, Does.Contain("nonexistent.json"));
			Assert.That(ex.FileName, Is.EqualTo("nonexistent.json"));
			Assert.That(ex.InnerException, Is.InstanceOf<FileNotFoundException>());
		}

		[Test]
		public void Load_WhenInvalidJson_ThrowsConfigurationException()
		{
			// Arrange
			File.WriteAllText(_testFilePath, "{ invalid json syntax }");
			var source = new EncryptedJsonConfigurationSource
			{
				Path = _testFilePath,
				Key = "TestKey123456",
				Optional = false
			};
			source.ResolveFileProvider();
			var provider = new EncryptedJsonConfigurationProvider(source);

			// Act & Assert
			var ex = Assert.Throws<ConfigurationException>(() => provider.Load());
			Assert.That(ex.Message, Does.Contain("invalid JSON"));
			Assert.That(ex.Message, Does.Contain("test.json"));
			Assert.That(ex.FileName, Is.EqualTo("test.json"));
			Assert.That(ex.InnerException, Is.InstanceOf<JsonException>());
		}

		[Test]
		public void Load_WhenDecryptionFails_ThrowsEncryptionException()
		{
			// Arrange - create a file with encrypted content using one key
			var encryptor1 = new Encryptor("CorrectKey123456");
			var encryptedValue = encryptor1.Encrypt("secret value");

			var json = "{\r\n\t\"Database\": {\r\n\t\t\"Password\": \"" + encryptedValue + "\"\r\n\t}\r\n}";
			File.WriteAllText(_testFilePath, json);

			// Try to decrypt with a different key
			var source = new EncryptedJsonConfigurationSource
			{
				Path = _testFilePath,
				Key = "WrongKey123456789",
				Optional = false
			};
			source.ResolveFileProvider();
			var provider = new EncryptedJsonConfigurationProvider(source);

			// Act & Assert
			var ex = Assert.Throws<EncryptionException>(() => provider.Load());
			Assert.That(ex.Message, Does.Contain("decrypt"));
			Assert.That(ex.Message, Does.Contain("test.json"));
			Assert.That(ex.Message, Does.Contain("Database:Password"));
			Assert.That(ex.FileName, Is.EqualTo("test.json"));
			Assert.That(ex.Key, Is.EqualTo("Database:Password"));
			Assert.That(ex.InnerException, Is.Not.Null);
		}

		[Test]
		public void Settings_WhenMountPathEmpty_ThrowsArgumentException()
		{
			// Act & Assert
			var ex = Assert.Throws<ArgumentException>(() =>
			{
				var settings = new Settings();
				settings.ConfigMountPath = "";
			});
			Assert.That(ex.Message, Does.Contain("mount path"));
			Assert.That(ex.ParamName, Is.EqualTo("ConfigMountPath"));
		}

		[Test]
		public void Settings_WhenFileNameEmpty_ThrowsArgumentException()
		{
			// Act & Assert
			var ex = Assert.Throws<ArgumentException>(() =>
			{
				var settings = new Settings();
				settings.FileName = "";
			});
			Assert.That(ex.Message, Does.Contain("name"));
			Assert.That(ex.ParamName, Is.EqualTo("FileName"));
		}

		[Test]
		public void Settings_WhenCurrentDirectoryEmpty_ThrowsArgumentException()
		{
			// Act & Assert
			var ex = Assert.Throws<ArgumentException>(() =>
			{
				var settings = new Settings();
				settings.CurrentDirectory = "";
			});
			Assert.That(ex.Message, Does.Contain("directory"));
			Assert.That(ex.ParamName, Is.EqualTo("CurrentDirectory"));
		}

		[Test]
		public void Settings_WhenKeyTooShort_ThrowsArgumentException()
		{
			// Act & Assert
			var ex = Assert.Throws<ArgumentException>(() =>
			{
				var settings = new Settings();
				settings.Key = "short";
			});
			Assert.That(ex.Message, Does.Contain("at least"));
			Assert.That(ex.ParamName, Is.EqualTo("Key"));
		}

		[Test]
		public void AddMountConfiguration_WhenBuilderIsNull_ThrowsArgumentNullException()
		{
			// Arrange
			IConfigurationBuilder builder = null!;
			var provider = new SettingsProvider();

			// Act & Assert
			Assert.Throws<ArgumentNullException>(() =>
				Microsoft.Extensions.DependencyInjection.ConfigurationExtension.AddMountConfiguration(builder, provider));
		}

		[Test]
		public void AddMountConfiguration_WhenProviderIsNull_ThrowsArgumentNullException()
		{
			// Arrange
			var builder = new ConfigurationBuilder();
			SettingsProvider provider = null!;

			// Act & Assert
			Assert.Throws<ArgumentNullException>(() =>
				Microsoft.Extensions.DependencyInjection.ConfigurationExtension.AddMountConfiguration(builder, provider));
		}

		[Test]
		public void AddMountConfiguration_WhenFilenameIsNull_ThrowsArgumentNullException()
		{
			// Arrange
			var builder = new ConfigurationBuilder();
			var provider = new SettingsProvider();
			string filename = null!;

			// Act & Assert
			Assert.Throws<ArgumentNullException>(() =>
				Microsoft.Extensions.DependencyInjection.ConfigurationExtension.AddMountConfiguration(builder, provider, filename));
		}

		[Test]
		public void ConfigurationException_WithContext_ContainsAllProperties()
		{
			// Arrange
			var innerException = new FileNotFoundException("Test file not found");
			var mountPath = "/config";
			var fileName = "appsettings.json";
			var message = "Test configuration error";

			// Act
			var ex = new ConfigurationException(message, mountPath, fileName, innerException);

			// Assert
			Assert.That(ex.Message, Is.EqualTo(message));
			Assert.That(ex.MountPath, Is.EqualTo(mountPath));
			Assert.That(ex.FileName, Is.EqualTo(fileName));
			Assert.That(ex.InnerException, Is.EqualTo(innerException));
		}

		[Test]
		public void EncryptionException_WithContext_ContainsAllProperties()
		{
			// Arrange
			var innerException = new CryptographicException("Test crypto error");
			var mountPath = "/config";
			var fileName = "secrets.json";
			var key = "Database:Password";
			var message = "Test encryption error";

			// Act
			var ex = new EncryptionException(message, mountPath, fileName, key, innerException);

			// Assert
			Assert.That(ex.Message, Is.EqualTo(message));
			Assert.That(ex.MountPath, Is.EqualTo(mountPath));
			Assert.That(ex.FileName, Is.EqualTo(fileName));
			Assert.That(ex.Key, Is.EqualTo(key));
			Assert.That(ex.InnerException, Is.EqualTo(innerException));
		}

		[Test]
		public void EncryptionException_InheritsFromConfigurationException()
		{
			// Arrange & Act
			var ex = new EncryptionException("Test message");

			// Assert
			Assert.That(ex, Is.InstanceOf<ConfigurationException>());
		}

		[Test]
		public void Load_WhenDecryptionSucceeds_DoesNotThrow()
		{
			// Arrange
			var encryptor = new Encryptor("TestKey123456");
			var encryptedValue = encryptor.Encrypt("secret value");

			var json = "{\r\n\t\"Database\": {\r\n\t\t\"Password\": \"" + encryptedValue + "\"\r\n\t}\r\n}";
			File.WriteAllText(_testFilePath, json);

			var source = new EncryptedJsonConfigurationSource
			{
				Path = _testFilePath,
				Key = "TestKey123456",
				Optional = false
			};
			source.ResolveFileProvider();
			var provider = new EncryptedJsonConfigurationProvider(source);

			// Act & Assert
			Assert.DoesNotThrow(() => provider.Load());

			// Verify decryption worked
			Assert.That(provider.TryGet("Database:Password", out var value), Is.True);
			Assert.That(value, Is.EqualTo("secret value"));
		}
	}
}
