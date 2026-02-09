using Microsoft.Extensions.Configuration;

namespace Voyager.Configuration.MountPath.Test
{

  public class ConfigUser
  {
    private readonly IConfiguration _configuration;

    public ConfigUser(IConfiguration configuration)
    {
      _configuration = configuration;
    }

    public virtual string GetTestSetting()
    {
      return _configuration["TestSetting"]!;
    }

    public string GetEnvironmentSetting()
    {
      return _configuration["EnvironmentSetting"]!;
    }

  }
}
