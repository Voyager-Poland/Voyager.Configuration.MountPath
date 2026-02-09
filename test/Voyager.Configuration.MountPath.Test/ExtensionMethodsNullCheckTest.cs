using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	/// <summary>
	/// Tests for extension methods null parameter validation.
	/// </summary>
	[TestFixture]
	public class ExtensionMethodsNullCheckTest
	{
		[Test]
		public void AddMountConfiguration_WithNullBuilder_ThrowsArgumentNullException()
		{
			IConfigurationBuilder builder = null!;
			var provider = new SettingsProvider();

			Assert.Throws<ArgumentNullException>(() =>
				builder.AddMountConfiguration(provider, "test"));
		}

		[Test]
		public void AddMountConfiguration_WithNullProvider_ThrowsArgumentNullException()
		{
			var builder = new ConfigurationBuilder();
			SettingsProvider provider = null!;

			Assert.Throws<ArgumentNullException>(() =>
				builder.AddMountConfiguration(provider, "test"));
		}

		[Test]
		public void AddMountConfiguration_WithNullFilename_ThrowsArgumentNullException()
		{
			var builder = new ConfigurationBuilder();
			var provider = new SettingsProvider();

			Assert.Throws<ArgumentNullException>(() =>
				builder.AddMountConfiguration(provider, new string[] { null! }));
		}

		[Test]
		public void AddMountConfiguration_WithNullSettings_ThrowsArgumentNullException()
		{
			var builder = new ConfigurationBuilder();
			Settings settings = null!;

			Assert.Throws<ArgumentNullException>(() =>
				builder.AddMountConfiguration(settings));
		}

		[Test]
		public void AddMountConfiguration_WithNullAction_ThrowsArgumentNullException()
		{
			var builder = new ConfigurationBuilder();
			Action<Settings> action = null!;

			Assert.Throws<ArgumentNullException>(() =>
				builder.AddMountConfiguration(action));
		}

		[Test]
		public void AddEncryptedMountConfiguration_WithNullBuilder_ThrowsArgumentNullException()
		{
			IConfigurationBuilder builder = null!;
			var provider = new SettingsProvider();

			Assert.Throws<ArgumentNullException>(() =>
				builder.AddEncryptedMountConfiguration("key", provider, "test"));
		}

		[Test]
		public void AddEncryptedMountConfiguration_WithNullKey_ThrowsArgumentNullException()
		{
			var builder = new ConfigurationBuilder();
			var provider = new SettingsProvider();

			Assert.Throws<ArgumentNullException>(() =>
				builder.AddEncryptedMountConfiguration(null!, provider, "test"));
		}

		[Test]
		public void AddEncryptedMountConfiguration_WithEmptyKey_ThrowsArgumentException()
		{
			var builder = new ConfigurationBuilder();
			var provider = new SettingsProvider();

			Assert.Throws<ArgumentException>(() =>
				builder.AddEncryptedMountConfiguration(string.Empty, provider, "test"));
		}

		[Test]
		public void AddEncryptedMountConfiguration_WithNullProvider_ThrowsArgumentNullException()
		{
			var builder = new ConfigurationBuilder();
			SettingsProvider provider = null!;

			Assert.Throws<ArgumentNullException>(() =>
				builder.AddEncryptedMountConfiguration("key", provider, "test"));
		}

		[Test]
		public void AddEncryptedMountConfiguration_WithNullFilename_ThrowsArgumentNullException()
		{
			var builder = new ConfigurationBuilder();
			var provider = new SettingsProvider();

			Assert.Throws<ArgumentNullException>(() =>
				builder.AddEncryptedMountConfiguration("key", provider, new string[] { null! }));
		}

		[Test]
		public void AddEncryptedMountConfiguration_WithNullSettings_ThrowsArgumentNullException()
		{
			var builder = new ConfigurationBuilder();
			Settings settings = null!;

			Assert.Throws<ArgumentNullException>(() =>
				builder.AddEncryptedMountConfiguration(settings));
		}

		[Test]
		public void AddEncryptedMountConfiguration_WithNullAction_ThrowsArgumentNullException()
		{
			var builder = new ConfigurationBuilder();
			Action<Settings> action = null!;

			Assert.Throws<ArgumentNullException>(() =>
				builder.AddEncryptedMountConfiguration(action));
		}

		[Test]
		public void AddEncryptedJsonFile_WithNullBuilder_ThrowsArgumentNullException()
		{
			IConfigurationBuilder builder = null!;

			Assert.Throws<ArgumentNullException>(() =>
				builder.AddEncryptedJsonFile("path", "key", optional: false, reloadOnChange: false));
		}

		[Test]
		public void AddEncryptedJsonFile_WithNullPath_ThrowsArgumentException()
		{
			var builder = new ConfigurationBuilder();

			Assert.Throws<ArgumentException>(() =>
				builder.AddEncryptedJsonFile(null!, "key", optional: false, reloadOnChange: false));
		}

		[Test]
		public void AddEncryptedJsonFile_WithEmptyPath_ThrowsArgumentException()
		{
			var builder = new ConfigurationBuilder();

			Assert.Throws<ArgumentException>(() =>
				builder.AddEncryptedJsonFile(string.Empty, "key", optional: false, reloadOnChange: false));
		}

		[Test]
		public void AddVoyagerConfiguration_WithNullServices_ThrowsArgumentNullException()
		{
			IServiceCollection services = null!;

			Assert.Throws<ArgumentNullException>(() =>
				services.AddVoyagerConfiguration());
		}

		[Test]
		public void AddVoyagerConfiguration_Generic_WithNullServices_ThrowsArgumentNullException()
		{
			IServiceCollection services = null!;

			Assert.Throws<ArgumentNullException>(() =>
				services.AddVoyagerConfiguration<SettingsProvider>());
		}

		[Test]
		public void GetSettingsProvider_WithNullEnvironment_ThrowsArgumentNullException()
		{
			IHostEnvironment environment = null!;

			Assert.Throws<ArgumentNullException>(() =>
				environment.GetSettingsProvider());
		}

		[Test]
		public void GetSettingsProviderForce_WithNullEnvironment_ThrowsArgumentNullException()
		{
			IHostEnvironment environment = null!;

			Assert.Throws<ArgumentNullException>(() =>
				environment.GetSettingsProviderForce());
		}

		[Test]
		public void AddMountConfiguration_WithValidParameters_DoesNotThrow()
		{
			var builder = new ConfigurationBuilder();
			var provider = new SettingsProvider();

			Assert.DoesNotThrow(() =>
				builder.AddMountConfiguration(provider, "appsettings"));
		}

		[Test]
		public void AddMountConfiguration_WithAction_ValidatesNullParameters()
		{
			var builder = new ConfigurationBuilder();

			// This should not throw - the action itself is not null
			Assert.DoesNotThrow(() =>
				builder.AddMountConfiguration(settings =>
				{
					settings.FileName = "test";
				}));
		}

		[Test]
		public void AddVoyagerConfiguration_WithValidServices_DoesNotThrow()
		{
			var services = new ServiceCollection();

			Assert.DoesNotThrow(() =>
				services.AddVoyagerConfiguration());
		}

		[Test]
		public void AddVoyagerConfiguration_Generic_WithValidServices_DoesNotThrow()
		{
			var services = new ServiceCollection();

			Assert.DoesNotThrow(() =>
				services.AddVoyagerConfiguration<SettingsProvider>());
		}

		[Test]
		public void GetSettingsProvider_WithValidEnvironment_DoesNotThrow()
		{
			var builder = Host.CreateDefaultBuilder();
			using var host = builder.Build();
			var environment = host.Services.GetRequiredService<IHostEnvironment>();

			Assert.DoesNotThrow(() =>
				environment.GetSettingsProvider());
		}

		[Test]
		public void GetSettingsProviderForce_WithValidEnvironment_DoesNotThrow()
		{
			var builder = Host.CreateDefaultBuilder();
			using var host = builder.Build();
			var environment = host.Services.GetRequiredService<IHostEnvironment>();

			Assert.DoesNotThrow(() =>
				environment.GetSettingsProviderForce());
		}
	}
}
