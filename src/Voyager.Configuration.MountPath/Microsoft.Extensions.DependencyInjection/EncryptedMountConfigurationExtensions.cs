using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Voyager.Configuration.MountPath;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Extension methods for adding encrypted configuration from mount paths.
	/// Provides high-level API for loading encrypted JSON files from configurable mount directories.
	/// </summary>
	/// <remarks>
	/// This class follows the Single Responsibility Principle by handling only mount path configuration.
	/// For low-level encrypted JSON file operations, use <see cref="EncryptedJsonFileExtensions"/>.
	/// </remarks>
	public static class EncryptedMountConfigurationExtensions
	{
		/// <summary>
		/// Adds encrypted JSON configuration files from a mount path using the specified settings.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="settings">The settings containing file name, mount path, and encryption key.</param>
		/// <returns>The configuration builder for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when configurationBuilder or settings is null.</exception>
		public static IConfigurationBuilder AddEncryptedMountConfiguration(this IConfigurationBuilder configurationBuilder, Settings settings)
		{
			if (configurationBuilder == null)
				throw new ArgumentNullException(nameof(configurationBuilder));
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));

			configurationBuilder.SetBasePath(Path.Combine(settings.CurrentDirectory, settings.ConfigMountPath));
			EncryptedJsonFileExtensions.AddEncryptedJsonFile(configurationBuilder, $"{settings.FileName}.json", settings.Key, optional: false, reloadOnChange: true);
			EncryptedJsonFileExtensions.AddEncryptedJsonFile(configurationBuilder, $"{settings.FileName}.{settings.HostingName}.json", settings.Key, optional: settings.Optional, reloadOnChange: true);
			return configurationBuilder;
		}

		/// <summary>
		/// Adds an encrypted JSON configuration file from a mount path.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="key">The encryption key.</param>
		/// <param name="provider">The settings provider.</param>
		/// <param name="filename">The configuration file name (without extension).</param>
		/// <returns>The configuration builder for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
		public static IConfigurationBuilder AddEncryptedMountConfiguration(this IConfigurationBuilder configurationBuilder, string key, SettingsProvider provider, string filename)
		{
			if (configurationBuilder == null)
				throw new ArgumentNullException(nameof(configurationBuilder));
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));
			if (filename == null)
				throw new ArgumentNullException(nameof(filename));

			var setting = provider.GetSettings(filename);
			setting.Key = key;
			return AddEncryptedMountConfiguration(configurationBuilder, setting);
		}

		/// <summary>
		/// Adds multiple encrypted JSON configuration files from a mount path.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="key">The encryption key.</param>
		/// <param name="provider">The settings provider.</param>
		/// <param name="filenames">The configuration file names (without extensions).</param>
		/// <returns>The configuration builder for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
		public static IConfigurationBuilder AddEncryptedMountConfiguration(this IConfigurationBuilder configurationBuilder, string key, SettingsProvider provider, params string[] filenames)
		{
			if (configurationBuilder == null)
				throw new ArgumentNullException(nameof(configurationBuilder));
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));
			if (filenames == null)
				throw new ArgumentNullException(nameof(filenames));

			foreach (var filename in filenames)
			{
				var setting = provider.GetSettings(filename);
				setting.Key = key;
				AddEncryptedMountConfiguration(configurationBuilder, setting);
			}
			return configurationBuilder;
		}

		/// <summary>
		/// Adds encrypted JSON configuration files from a mount path using a configuration action.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="configureSettings">Action to configure the settings.</param>
		/// <returns>The configuration builder for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
		public static IConfigurationBuilder AddEncryptedMountConfiguration(this IConfigurationBuilder configurationBuilder, Action<Settings> configureSettings)
		{
			if (configurationBuilder == null)
				throw new ArgumentNullException(nameof(configurationBuilder));
			if (configureSettings == null)
				throw new ArgumentNullException(nameof(configureSettings));

			var setting = SettingsProvider.PrepareDefault();
			configureSettings.Invoke(setting);
			return AddEncryptedMountConfiguration(configurationBuilder, setting);
		}
	}
}
