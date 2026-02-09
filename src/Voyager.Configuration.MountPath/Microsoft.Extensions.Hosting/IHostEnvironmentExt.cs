using Voyager.Configuration.MountPath;

namespace Microsoft.Extensions.Hosting
{
	/// <summary>
	/// Extension methods for <see cref="IHostEnvironment"/> to create settings providers.
	/// </summary>
	public static class IHostEnvironmentExt
	{
		/// <summary>
		/// Gets a settings provider that uses the hosting environment's settings.
		/// </summary>
		/// <param name="env">The hosting environment.</param>
		/// <returns>A settings provider configured for the current hosting environment.</returns>
		/// <remarks>
		/// The environment-specific configuration file is optional by default.
		/// Use <see cref="GetSettingsProviderForce"/> to require the environment file.
		/// </remarks>
		public static SettingsProvider GetSettingsProvider(this IHostEnvironment env)
		{
			return new HostEnvironmentSettings(env);
		}

		/// <summary>
		/// Gets a settings provider that requires environment-specific configuration files.
		/// </summary>
		/// <param name="env">The hosting environment.</param>
		/// <returns>A settings provider that requires environment-specific files.</returns>
		/// <remarks>
		/// This method sets Optional = false, which means the environment-specific
		/// configuration file (e.g., appsettings.Production.json) must exist.
		/// </remarks>
		public static SettingsProvider GetSettingsProviderForce(this IHostEnvironment env)
		{
			return new ForceHostEnvironmentSettings(env);
		}
	}
}
