using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	/// <summary>
	/// Tests for SettingsProvider edge cases including null and empty paths.
	/// </summary>
	[TestFixture]
	public class SettingsProviderEdgeCasesTest
	{
		[Test]
		public void GetSettings_WithNullFilename_ThrowsArgumentNullException()
		{
			var provider = new SettingsProvider();

			Assert.Throws<ArgumentNullException>(() => provider.GetSettings(null!));
		}

		[Test]
		public void GetSettings_WithEmptyFilename_ThrowsArgumentException()
		{
			var provider = new SettingsProvider();

			Assert.Throws<ArgumentException>(() => provider.GetSettings(string.Empty));
		}

		[Test]
		public void GetSettings_WithWhitespaceFilename_ThrowsArgumentException()
		{
			var provider = new SettingsProvider();

			Assert.Throws<ArgumentException>(() => provider.GetSettings("   "));
		}

		[Test]
		public void GetSettings_WithValidFilename_ReturnsSettings()
		{
			var provider = new SettingsProvider();

			var settings = provider.GetSettings("appsettings");

			Assert.That(settings, Is.Not.Null);
			Assert.That(settings.FileName, Is.EqualTo("appsettings"));
		}

		[Test]
		public void GetSettings_UsesDefaultConfigMountPath()
		{
			var provider = new SettingsProvider();

			var settings = provider.GetSettings("test");

			Assert.That(settings.ConfigMountPath, Is.EqualTo("config"));
		}

		[Test]
		public void GetSettings_UsesCurrentDirectory()
		{
			var provider = new SettingsProvider();

			var settings = provider.GetSettings("test");

			Assert.That(settings.CurrentDirectory, Is.Not.Null);
			Assert.That(settings.CurrentDirectory, Is.Not.Empty);
		}

		[Test]
		public void GetSettings_DefaultOptionalIsTrue()
		{
			var provider = new SettingsProvider();

			var settings = provider.GetSettings("test");

			Assert.That(settings.Optional, Is.True);
		}

		[Test]
		public void GetSettingsProviderForce_SetsOptionalToFalse()
		{
			var hostEnvironment = CreateMockHostingEnvironment();

			var provider = hostEnvironment.GetSettingsProviderForce();
			var settings = provider.GetSettings("test");

			Assert.That(settings.Optional, Is.False);
		}

		[Test]
		public void GetSettingsProvider_SetsOptionalToTrue()
		{
			var hostEnvironment = CreateMockHostingEnvironment();

			var provider = hostEnvironment.GetSettingsProvider();
			var settings = provider.GetSettings("test");

			Assert.That(settings.Optional, Is.True);
		}

		[Test]
		public void GetSettingsProvider_WithNullHostingEnvironment_ThrowsArgumentNullException()
		{
			IHostEnvironment hostEnvironment = null!;

			Assert.Throws<ArgumentNullException>(() => hostEnvironment.GetSettingsProvider());
		}

		[Test]
		public void GetSettingsProviderForce_WithNullHostingEnvironment_ThrowsArgumentNullException()
		{
			IHostEnvironment hostEnvironment = null!;

			Assert.Throws<ArgumentNullException>(() => hostEnvironment.GetSettingsProviderForce());
		}

		[Test]
		public void PrepareDefault_ReturnsValidSettings()
		{
			var settings = SettingsProvider.PrepareDefault();

			Assert.That(settings, Is.Not.Null);
			Assert.That(settings.FileName, Is.EqualTo("appsettings"));
			Assert.That(settings.ConfigMountPath, Is.EqualTo("config"));
			Assert.That(settings.Optional, Is.True);
		}

		[Test]
		public void Settings_CanModifyConfigMountPath()
		{
			var settings = SettingsProvider.PrepareDefault();

			settings.ConfigMountPath = "custom-config";

			Assert.That(settings.ConfigMountPath, Is.EqualTo("custom-config"));
		}

		[Test]
		public void Settings_CanModifyFileName()
		{
			var settings = SettingsProvider.PrepareDefault();

			settings.FileName = "custom-file";

			Assert.That(settings.FileName, Is.EqualTo("custom-file"));
		}

		[Test]
		public void Settings_CanModifyOptional()
		{
			var settings = SettingsProvider.PrepareDefault();

			settings.Optional = false;

			Assert.That(settings.Optional, Is.False);
		}

		[Test]
		public void Settings_CanModifyHostingName()
		{
			var settings = SettingsProvider.PrepareDefault();

			settings.HostingName = "CustomEnvironment";

			Assert.That(settings.HostingName, Is.EqualTo("CustomEnvironment"));
		}

		private static IHostEnvironment CreateMockHostingEnvironment()
		{
			var builder = Host.CreateDefaultBuilder();
			using var host = builder.Build();
			return host.Services.GetService(typeof(IHostEnvironment)) as IHostEnvironment ?? throw new InvalidOperationException("Cannot create host environment");
		}
	}
}
