using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	/// <summary>
	/// Base class for configuration tests using composition instead of deep inheritance.
	/// </summary>
	[TestFixture]
	public abstract class ConfigurationTestBase
	{
		private IHost host;

		protected IConfiguration Configuration => host.Services.GetRequiredService<IConfiguration>();
		protected ConfigUser ConfigUser => host.Services.GetRequiredService<ConfigUser>();

		[SetUp]
		public void SetUp()
		{
			var builder = Host.CreateDefaultBuilder(null);
			builder.ConfigureAppConfiguration((context, config) =>
			{
				Console.WriteLine(context.HostingEnvironment.EnvironmentName);
				ConfigureHost(context, config);
			});
			builder.ConfigureServices(services => services.AddTransient<ConfigUser>());
			host = builder.Build();
		}

		[TearDown]
		public void TearDown()
		{
			host?.Dispose();
		}

		/// <summary>
		/// Override to configure the host and configuration sources.
		/// </summary>
		protected abstract void ConfigureHost(HostBuilderContext context, IConfigurationBuilder config);
	}
}
