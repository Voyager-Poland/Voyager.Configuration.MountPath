using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;


namespace Voyager.Configuration.MountPath.Test;

public class SettingProviderFromHost : IHostEnvironment
{
	private SettingsProvider _provider;
	private const string HOSTINGNAME = "MyVariableValue";

	[SetUp]
	public void Setup()
	{
		EnvironmentName = HOSTINGNAME;
		ApplicationName = AppDomain.CurrentDomain.FriendlyName;
		ContentRootPath = Directory.GetCurrentDirectory();
		_provider = this.GetSettingsProvider();
	}

	[Test]
	public void CheckSettings()
	{
		Settings settings = _provider.GetSettings();
		Assert.IsNotNull(settings);
		Assert.That(settings.HostingName, Is.EqualTo(HOSTINGNAME));
		Assert.That(settings.CurrentDirectory, Is.EqualTo(Directory.GetCurrentDirectory()));
	}


	public string EnvironmentName { get; set; }
	public string ApplicationName { get; set; }
	public string ContentRootPath { get; set; }



	public IFileProvider ContentRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}
