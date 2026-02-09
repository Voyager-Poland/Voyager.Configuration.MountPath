using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;


namespace Voyager.Configuration.MountPath.Test;

public class SettingProviderFromHost : IHostEnvironment
{
	private SettingsProvider provider;
	private const string HOSTINGNAME = "MyVariableValue";

	[SetUp]
	public void Setup()
	{
		this.EnvironmentName = HOSTINGNAME;
		this.ApplicationName = AppDomain.CurrentDomain.FriendlyName;
		this.ContentRootPath = Directory.GetCurrentDirectory();
		provider = this.GetSettingsProvider();
	}

	[Test]
	public void CheckSettings()
	{
		Settings settings = provider.GetSettings();
		Assert.IsNotNull(settings);
		Assert.That(settings.HostingName, Is.EqualTo(HOSTINGNAME));
		Assert.That(settings.CurrentDirectory, Is.EqualTo(Directory.GetCurrentDirectory()));
	}


	public string EnvironmentName { get; set; }
	public string ApplicationName { get; set; }
	public string ContentRootPath { get; set; }



	public IFileProvider ContentRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}