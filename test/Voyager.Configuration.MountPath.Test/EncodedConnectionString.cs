using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	/// <summary>
	/// Tests loading encrypted connection strings from configuration.
	/// </summary>
	[TestFixture]
	internal class EncodedConnectionString : ConfigurationTestBase
	{
		private const string EncryptionKey = "PowaznyTestks123456722228";

		protected override void ConfigureHost(HostBuilderContext context, IConfigurationBuilder config)
		{
			config.AddMountConfiguration(settings =>
			{
				settings.HostingName = "MyEnv";
				settings.Optional = false;
			});
			config.AddMountConfiguration(context.HostingEnvironment.GetSettingsProvider(), "srp");
			config.AddEncryptedMountConfiguration(EncryptionKey, context.HostingEnvironment.GetSettingsProvider(), "connectionstring");
		}

		[Test]
		public void LoadConfig_WithEncryptedConnectionStrings_DecryptsValues()
		{
			Assert.That(Configuration["EnvironmentSetting"], Is.EqualTo("specific"));
			Assert.That(Configuration["spr"], Is.EqualTo("yes"));
			Assert.That(Configuration.GetConnectionString("db1"), Is.EqualTo("tekst to encode może jednak ma być dłuższy"));
			Assert.That(Configuration.GetConnectionString("db2"), Is.EqualTo("tekst to encode może jednak ma być dłuższy"));
		}
	}
}
