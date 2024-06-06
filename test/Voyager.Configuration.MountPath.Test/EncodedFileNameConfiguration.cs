using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath.Test
{
  class EncodedFileNameConfiguration : FileNameConfiguration
  {
    protected override void AddFileConfig(HostBuilderContext hostingConfiguration, IConfigurationBuilder config)
    {
      base.AddFileConfig(hostingConfiguration, config);
      config.AddEncryptedMountConfiguration("PowaznyTestks123456722228", hostingConfiguration.HostingEnvironment.GetSettingsProvider(), "encoded");
    }

    public override void TestNewConfig()
    {
      base.TestNewConfig();
    }

    protected override void CheckFile(IConfiguration config)
    {
      base.CheckFile(config);
      Assert.That(config["values"], Is.EqualTo("tekst to encode może jednak ma być dłuższy"));
    }
  }


  class EncodedConnectioString : FileNameConfiguration
  {
    protected override void AddFileConfig(HostBuilderContext hostingConfiguration, IConfigurationBuilder config)
    {
      base.AddFileConfig(hostingConfiguration, config);
      config.AddEncryptedMountConfiguration("PowaznyTestks123456722228", hostingConfiguration.HostingEnvironment.GetSettingsProvider(), "connectionstring");
    }

    public override void TestNewConfig()
    {
      base.TestNewConfig();
    }

    protected override void CheckFile(IConfiguration config)
    {
      base.CheckFile(config);
      Assert.That(config.GetConnectionString("db1"), Is.EqualTo("tekst to encode może jednak ma być dłuższy"));
      Assert.That(config.GetConnectionString("db2"), Is.EqualTo("tekst to encode może jednak ma być dłuższy"));
    }
  }
}