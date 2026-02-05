using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	internal class FileNameEmptyConfiguration : SpecificConfiguration
	{
		protected override void AddConfig(HostBuilderContext hostingConfiguration, IConfigurationBuilder config)
		{
			base.AddConfig(hostingConfiguration, config);
			AddFileConfig(hostingConfiguration, config);
		}

		protected virtual void AddFileConfig(HostBuilderContext hostingConfiguration, IConfigurationBuilder config)
		{
		}

		[Test]
		public virtual void TestNewConfig()
		{
			var config = this.GetConfiguration();
			CheckFile(config);
		}

		protected virtual void CheckFile(IConfiguration config)
		{
			Assert.That(config["EnvironmentSetting"], Is.EqualTo("specific"));
		}
	}
}