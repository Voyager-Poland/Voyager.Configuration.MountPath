using System.IO;

namespace Voyager.Configuration.MountPath
{
	/// <summary>
	/// Configuration settings for mount path-based configuration loading.
	/// </summary>
	public class Settings
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Settings"/> class with default values.
		/// </summary>
		public Settings()
		{
			FileName = SettingsDefaults.DefaultFileName;
			ConfigMountPath = SettingsDefaults.DefaultConfigMountPath;
			HostingName = SettingsDefaults.DefaultHostingName;
			CurrentDirectory = Directory.GetCurrentDirectory();
			Optional = true;
		}

		/// <summary>
		/// Gets or sets the current working directory for configuration files.
		/// </summary>
		public string CurrentDirectory { get; set; }

		/// <summary>
		/// Gets or sets the configuration file name (without extension).
		/// </summary>
		public string FileName { get; set; }

		/// <summary>
		/// Gets or sets the mount path where configuration files are located.
		/// </summary>
		public string ConfigMountPath { get; set; }

		/// <summary>
		/// Gets or sets the hosting environment name (e.g., Development, Production).
		/// </summary>
		public string HostingName { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the configuration file is optional.
		/// </summary>
		public bool Optional { get; set; }

		/// <summary>
		/// Gets or sets the encryption key for encrypted configuration values.
		/// </summary>
		public string Key { get; set; }
	}
}
