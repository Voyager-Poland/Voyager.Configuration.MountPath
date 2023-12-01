using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	abstract class SpecyficConfiguration : ConfigureHosting
	{

		protected override void AddConfig(HostBuilderContext hostingConfiguration, IConfigurationBuilder config)
		{
			config.AddMountConfiguration(settings =>
			{
				settings.HostingName = "MyEnv";
				settings.Optional = false;
			});
		}

		protected override string GetEnvValue() => "specific";
	}
}