using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	internal class ConfigureHosting
	{
		private IHost host;

		[SetUp]
		public void Setup()
		{
			var builder = Host.CreateDefaultBuilder(null);
			PrepareConfiguration(builder);
			builder.ConfigureServices(AddServicess);
			this.host = builder.Build();
		}

		protected virtual void AddServicess(IServiceCollection services) => services.AddTransient<ConfigUser>();

		protected IConfiguration GetConfiguration() => host.Services.GetService<IConfiguration>()!;

		[TearDown]
		public void TearDown()
		{
			host?.Dispose();
		}

		[Test]
		public void GetConfigValue()
		{
			ConfigUser configUser = host.Services.GetService<ConfigUser>()!;
			Compare(configUser.GetTestSetting(), "For all");
			Compare(configUser.GetEnvironmentSetting(), GetEnvValue());
		}


		private void PrepareConfiguration(IHostBuilder builder) => builder.ConfigureAppConfiguration(AddConfig);

		protected virtual void AddConfig(HostBuilderContext hostingConfiguration, IConfigurationBuilder config)
		{
			Console.WriteLine(hostingConfiguration.HostingEnvironment.EnvironmentName);
			config.AddMountConfiguration(hostingConfiguration.HostingEnvironment.GetSettingsProvider());
		}
		protected virtual string GetEnvValue() => "int value";


		private void Compare(string output, string expected)
		{
			Assert.That(output, Is.EqualTo(expected));
		}

	}
}
