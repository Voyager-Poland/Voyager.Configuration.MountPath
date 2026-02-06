using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
	/// <summary>
	/// Tests loading encrypted configuration values from a named file.
	/// </summary>
	[TestFixture]
	internal class EncodedFileNameConfiguration : ConfigurationTestBase
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
			EncryptedMountConfigurationExtensions.AddEncryptedMountConfiguration(config, EncryptionKey, context.HostingEnvironment.GetSettingsProvider(), "encoded");
		}

		[Test]
		public void LoadConfig_WithEncryptedFile_DecryptsValues()
		{
			Assert.That(Configuration["EnvironmentSetting"], Is.EqualTo("specific"));
			Assert.That(Configuration["spr"], Is.EqualTo("yes"));
			Assert.That(Configuration["values"], Is.EqualTo("tekst to encode może jednak ma być dłuższy"));
		}
	}
}