using System.IO;

namespace Voyager.Configuration.MountPath
{
	public class Settings
	{
		public Settings()
		{
			FileName = "appsettings";
			ConfigMountPath = "config";
			HostingName = "Development";
			CurrentDirectory = Directory.GetCurrentDirectory();
		}
		public string CurrentDirectory { get; set; }
		public string FileName { get; set; }
		public string ConfigMountPath { get; set; }
		public string HostingName { get; set; }
	}
}
