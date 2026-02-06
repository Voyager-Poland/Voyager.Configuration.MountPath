using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Voyager.Configuration.MountPath;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Extension methods for adding non-encrypted configuration from mount paths.
	/// </summary>
	/// <remarks>
	/// This class provides high-level API for loading plain JSON configuration files
	/// from configurable mount directories. Files are loaded conditionally based on
	/// environment (e.g., appsettings.json + appsettings.Production.json).
	/// </remarks>
	public static class ConfigurationExtension
	{
		/// <summary>
		/// Adds JSON configuration files from a mount path using the specified filename.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="provider">The settings provider.</param>
		/// <param name="filename">The configuration file name without extension (default: "appsettings").</param>
		/// <returns>The configuration builder for method chaining.</returns>
		/// <remarks>
		/// Loads two files:
		/// <list type="bullet">
		/// <item><description>{filename}.json (required)</description></item>
		/// <item><description>{filename}.{Environment}.json (optional by default)</description></item>
		/// </list>
		/// </remarks>
		public static IConfigurationBuilder AddMountConfiguration(this IConfigurationBuilder configurationBuilder, SettingsProvider provider, string filename = "appsettings")
		{
			var setting = provider.GetSettings(filename);
			return AddMountConfiguration(configurationBuilder, setting);
		}

		/// <summary>
		/// Adds multiple JSON configuration files from a mount path.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="provider">The settings provider.</param>
		/// <param name="filenames">The configuration file names without extensions.</param>
		/// <returns>The configuration builder for method chaining.</returns>
		/// <remarks>
		/// For each filename, loads: {filename}.json + {filename}.{Environment}.json
		/// This allows organizing configuration by concern (database, logging, services, etc.).
		/// </remarks>
		public static IConfigurationBuilder AddMountConfiguration(this IConfigurationBuilder configurationBuilder, SettingsProvider provider, params string[] filenames)
		{
			foreach (var filename in filenames)
			{
				var setting = provider.GetSettings(filename);
				AddMountConfiguration(configurationBuilder, setting);
			}
			return configurationBuilder;
		}

		/// <summary>
		/// Adds JSON configuration files from a mount path using a configuration action.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="actDeleg">Action to configure the settings.</param>
		/// <returns>The configuration builder for method chaining.</returns>
		/// <example>
		/// <code>
		/// config.AddMountConfiguration(settings =>
		/// {
		///     settings.FileName = "database";
		///     settings.Optional = false;
		/// });
		/// </code>
		/// </example>
		public static IConfigurationBuilder AddMountConfiguration(this IConfigurationBuilder configurationBuilder, Action<Settings> actDeleg)
		{
			var setting = SettingsProvider.PrepareDefault();
			actDeleg.Invoke(setting);
			return AddMountConfiguration(configurationBuilder, setting);
		}

		/// <summary>
		/// Adds JSON configuration files from a mount path using the specified settings.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="settings">The settings containing file name, mount path, and environment name.</param>
		/// <returns>The configuration builder for method chaining.</returns>
		public static IConfigurationBuilder AddMountConfiguration(this IConfigurationBuilder configurationBuilder, Settings settings)
		{
			configurationBuilder.SetBasePath(Path.Combine(settings.CurrentDirectory, settings.ConfigMountPath))
											.AddJsonFile($"{settings.FileName}.json", optional: false, reloadOnChange: true)
											.AddJsonFile($"{settings.FileName}.{settings.HostingName}.json", optional: settings.Optional, reloadOnChange: true);
			return configurationBuilder;
		}
	}
}
