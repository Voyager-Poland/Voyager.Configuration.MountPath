using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	class ForceSpecyficConfiguration : ConfigureHosting
	{
		protected override void AddConfig(HostBuilderContext hostingConfiguration, IConfigurationBuilder config)
		{
			hostingConfiguration.HostingEnvironment.EnvironmentName = "Development";
			config.AddMountConfiguration(hostingConfiguration.HostingEnvironment.GetSettingsProviderForce());
		}
		protected override string GetEnvValue() => "dev";

	}
}
