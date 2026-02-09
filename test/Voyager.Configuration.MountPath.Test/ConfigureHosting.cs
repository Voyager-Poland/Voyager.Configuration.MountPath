using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	/// <summary>
	/// Tests default mount configuration loading.
	/// </summary>
	[TestFixture]
	internal class ConfigureHosting : ConfigurationTestBase
	{
		protected override void ConfigureHost(HostBuilderContext context, IConfigurationBuilder config)
		{
			config.AddMountConfiguration(context.HostingEnvironment.GetSettingsProvider());
		}

		[Test]
		public void GetConfigValue_WithDefaultConfiguration_ReturnsExpectedValues()
		{
			Assert.That(ConfigUser.GetTestSetting(), Is.EqualTo("For all"));
			Assert.That(ConfigUser.GetEnvironmentSetting(), Is.EqualTo("int value"));
		}
	}
}
