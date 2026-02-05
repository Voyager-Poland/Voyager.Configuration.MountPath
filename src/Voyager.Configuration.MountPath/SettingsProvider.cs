namespace Voyager.Configuration.MountPath
{
	/// <summary>
	/// Provides configuration settings for mount path configuration.
	/// </summary>
	public class SettingsProvider : ISettingsProvider
	{
		/// <inheritdoc />
		public virtual Settings GetSettings(string filename = SettingsDefaults.DefaultFileName)
		{
			return new Settings
			{
				FileName = filename ?? SettingsDefaults.DefaultFileName,
			};
		}

		/// <summary>
		/// Creates default settings.
		/// </summary>
		internal static Settings PrepareDefault()
		{
			var provider = new SettingsProvider();
			return provider.GetSettings();
		}
	}
}
