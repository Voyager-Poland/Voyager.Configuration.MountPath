namespace Voyager.Configuration.MountPath
{
	/// <summary>
	/// Provides default values for configuration settings.
	/// </summary>
	public static class SettingsDefaults
	{
		/// <summary>
		/// Default configuration file name (without extension).
		/// </summary>
		public const string DefaultFileName = "appsettings";

		/// <summary>
		/// Default mount path for configuration files.
		/// </summary>
		public const string DefaultConfigMountPath = "config";

		/// <summary>
		/// Default hosting environment name.
		/// </summary>
		public const string DefaultHostingName = "Development";

		/// <summary>
		/// Minimum required length for encryption keys.
		/// </summary>
		public const int MinimumKeyLength = 8;
	}
}
