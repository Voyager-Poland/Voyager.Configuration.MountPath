using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	/// <summary>
	/// Tests loading configuration with additional named config file.
	/// </summary>
	[TestFixture]
	internal class FileNameConfiguration : ConfigurationTestBase
	{
		protected override void ConfigureHost(HostBuilderContext context, IConfigurationBuilder config)
		{
			config.AddMountConfiguration(settings =>
			{
				settings.HostingName = "MyEnv";
				settings.Optional = false;
			});
			config.AddMountConfiguration(context.HostingEnvironment.GetSettingsProvider(), "srp");
		}

		[Test]
		public void LoadConfig_WithNamedFile_ContainsAllValues()
		{
			Assert.That(Configuration["EnvironmentSetting"], Is.EqualTo("specific"));
			Assert.That(Configuration["spr"], Is.EqualTo("yes"));
		}
	}
}