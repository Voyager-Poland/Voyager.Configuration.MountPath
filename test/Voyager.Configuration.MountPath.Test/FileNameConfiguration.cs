using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	class FileNameConfiguration : SpecyficConfiguration
	{

		protected override void AddConfig(HostBuilderContext hostingConfiguration, IConfigurationBuilder config)
		{
			base.AddConfig(hostingConfiguration, config);
			config.AddMountConfiguration(hostingConfiguration.HostingEnvironment.GetSettingsProvider(), "srp");
		}

		[Test]
		public void TestNewConfig()
		{
			var config = this.GetConfiguration();
			Assert.That(config["spr"], Is.EqualTo("yes"));
		}

	}


}