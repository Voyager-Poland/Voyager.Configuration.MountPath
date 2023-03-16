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
		public String CurrentDirectory { get; set; }
		public String FileName { get; set; }
		public String ConfigMountPath { get; set; }
		public String HostingName { get; set; }
	}
}
