using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	/// <summary>
	/// Tests loading multiple configuration files at once.
	/// </summary>
	[TestFixture]
	internal class GroupFileNameConfiguration : ConfigurationTestBase
	{
		protected override void ConfigureHost(HostBuilderContext context, IConfigurationBuilder config)
		{
			config.AddMountConfiguration(settings =>
			{
				settings.HostingName = "MyEnv";
				settings.Optional = false;
			});
			config.AddMountConfiguration(context.HostingEnvironment.GetSettingsProvider(), "srp", "another");
		}

		[Test]
		public void LoadConfig_WithMultipleFiles_ContainsAllValues()
		{
			Assert.That(Configuration["EnvironmentSetting"], Is.EqualTo("specific"));
			Assert.That(Configuration["spr"], Is.EqualTo("yes"));
			Assert.That(Configuration["another"], Is.EqualTo("yes"));
		}
	}
}