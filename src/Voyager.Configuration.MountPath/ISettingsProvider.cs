namespace Voyager.Configuration.MountPath
{
	/// <summary>
	/// Defines the contract for providing configuration settings.
	/// </summary>
	public interface ISettingsProvider
	{
		/// <summary>
		/// Gets the settings for the specified configuration filename.
		/// </summary>
		/// <param name="filename">The base filename without extension (default: "appsettings").</param>
		/// <returns>The configuration settings.</returns>
		Settings GetSettings(string filename = "appsettings");
	}
}
