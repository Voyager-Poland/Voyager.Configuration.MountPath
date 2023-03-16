using Microsoft.Extensions.Hosting;

namespace Voyager.Configuration.MountPath
{
	internal class HostEnvironmentSettings : Voyager.Configuration.MountPath.SettingsProvider
	{
		private readonly IHostEnvironment hostEnvironment;

		public HostEnvironmentSettings(IHostEnvironment hostEnvironment)
		{
			this.hostEnvironment = hostEnvironment;
		}

		public override Settings GetSettings()
		{
			var settings = base.GetSettings();
			settings.HostingName = hostEnvironment.EnvironmentName;
			settings.CurrentDirectory = hostEnvironment.ContentRootPath;
			return settings;
		}
	}
}
