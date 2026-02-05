using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	internal class FileNameConfiguration : FileNameEmptyConfiguration
	{
		protected override void AddFileConfig(HostBuilderContext hostingConfiguration, IConfigurationBuilder config)
		{
			config.AddMountConfiguration(hostingConfiguration.HostingEnvironment.GetSettingsProvider(), "srp");
		}

		[Test]
		public override void TestNewConfig()
		{
			base.TestNewConfig();
		}

		protected override void CheckFile(IConfiguration config)
		{
			base.CheckFile(config);
			Assert.That(config["spr"], Is.EqualTo("yes"));
		}
	}
}