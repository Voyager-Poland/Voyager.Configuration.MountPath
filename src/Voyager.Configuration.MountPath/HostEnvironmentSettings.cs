using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath
{
	internal class HostEnvironmentSettings : Voyager.Configuration.MountPath.SettingsProvider
	{
		private readonly IHostEnvironment _hostEnvironment;

		public HostEnvironmentSettings(IHostEnvironment hostEnvironment)
		{
			_hostEnvironment = hostEnvironment;
		}

		public override Settings GetSettings(string filename = "appsettings")
		{
			var settings = base.GetSettings(filename);
			settings.HostingName = _hostEnvironment.EnvironmentName;
			settings.CurrentDirectory = _hostEnvironment.ContentRootPath;
			return settings;
		}
	}

	internal class ForceHostEnvironmentSettings : HostEnvironmentSettings
	{
		public ForceHostEnvironmentSettings(IHostEnvironment hostEnvironment) : base(hostEnvironment)
		{
		}

		public override Settings GetSettings(string filename = "appsettings")
		{
			var settings = base.GetSettings(filename);
			settings.Optional = false;
			return settings;
		}
	}
}
