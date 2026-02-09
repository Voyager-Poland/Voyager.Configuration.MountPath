using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	/// <summary>
	/// Tests forced configuration with explicitly set environment name.
	/// </summary>
	[TestFixture]
	internal class ForceSpecificConfiguration : ConfigurationTestBase
	{
		protected override void ConfigureHost(HostBuilderContext context, IConfigurationBuilder config)
		{
			context.HostingEnvironment.EnvironmentName = "dev";
			config.AddMountConfiguration(context.HostingEnvironment.GetSettingsProviderForce());
		}

		[Test]
		public void GetConfigValue_WithForcedEnvironment_ReturnsDevValue()
		{
			Assert.That(ConfigUser.GetTestSetting(), Is.EqualTo("For all"));
			Assert.That(ConfigUser.GetEnvironmentSetting(), Is.EqualTo("dev"));
		}
	}
}
