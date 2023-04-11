using Microsoft.Extensions.Configuration;

namespace Voyager.Configuration.MountPath.Test
{

	public class ConfigUser
	{
		private readonly IConfiguration configuration;

		public ConfigUser(IConfiguration configuration)
		{
			this.configuration = configuration;
		}

		public string GetTestSetting()
		{
			return configuration["TestSetting"]!;
		}

		public string GetEnvironmentSetting()
		{
			return configuration["EnvironmentSetting"]!;
		}

	}

}
