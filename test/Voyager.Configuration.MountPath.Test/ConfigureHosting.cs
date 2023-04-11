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

			builder.ConfigureServices(services =>
			{
				services.AddTransient<ConfigUser>();
			});
			this.host = builder.Build();
		}

		[TearDown]
		public void TearDown()
		{
			host?.Dispose();
		}

		[Test]
		public void GetConfigValue()
		{
			ConfigUser configUser = host.Services.GetService<ConfigUser>()!;
			CommonTest(configUser.GetTestSetting());
			EnvTest(configUser.GetEnvironmentSetting());
		}


		protected virtual void PrepareConfiguration(IHostBuilder builder)
		{
			builder.ConfigureAppConfiguration((hostingConfiguration, config) =>
			{
				config.AddMountConfiguration(hostingConfiguration.HostingEnvironment.GetSettingsProvider());
			});
		}

		protected virtual void EnvTest(string output)
		{
			Compare(output, "int value");
		}


		private void CommonTest(string output)
		{
			Compare(output, "For all");
		}

		protected void Compare(string output, string expected)
		{
			Assert.That(output, Is.EqualTo(expected));
		}

	}


	internal class SpecyficConfiguration : ConfigureHosting
	{

		protected override void PrepareConfiguration(IHostBuilder builder)
		{
			builder.ConfigureAppConfiguration((hostingConfiguration, config) =>
			{
				config.AddMountConfiguration(settings =>
				{
					settings.HostingName = "MyEnv";
					settings.Optional = false;
				});
			});
		}

		protected override void EnvTest(string output)
		{
			Compare(output, "specific");
		}
	}


	internal class ForceSpecyficConfiguration : ConfigureHosting
	{
		protected override void PrepareConfiguration(IHostBuilder builder)
		{
			builder.ConfigureAppConfiguration((hostingConfiguration, config) =>
			{
				hostingConfiguration.HostingEnvironment.EnvironmentName = "Development";
				config.AddMountConfiguration(hostingConfiguration.HostingEnvironment.GetSettingsProviderForce());
			});
		}

		protected override void EnvTest(string output)
		{
			Assert.That(output, Is.EqualTo("dev"));
		}
	}
}
