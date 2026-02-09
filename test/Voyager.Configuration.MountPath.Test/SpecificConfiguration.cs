using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	/// <summary>
	/// Tests configuration with custom environment name.
	/// </summary>
	[TestFixture]
	internal class SpecificConfiguration : ConfigurationTestBase
	{
		protected override void ConfigureHost(HostBuilderContext context, IConfigurationBuilder config)
		{
			config.AddMountConfiguration(settings =>
			{
				settings.HostingName = "MyEnv";
				settings.Optional = false;
			});
		}

		[Test]
		public void GetConfigValue_WithCustomEnvironment_ReturnsSpecificValue()
		{
			Assert.That(Configuration["EnvironmentSetting"], Is.EqualTo("specific"));
		}
	}
}