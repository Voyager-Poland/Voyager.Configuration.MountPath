using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;
using Voyager.Configuration.MountPath;
using Voyager.Configuration.MountPath.Encryption;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Extension methods for adding encrypted configuration.
	/// This class is obsolete and will be removed in a future version.
	/// </summary>
	/// <remarks>
	/// This class has been split into two separate classes following the Single Responsibility Principle:
	/// <list type="bullet">
	/// <item><description>Use <see cref="EncryptedMountConfigurationExtensions"/> for AddEncryptedMountConfiguration methods.</description></item>
	/// <item><description>Use <see cref="EncryptedJsonFileExtensions"/> for AddEncryptedJsonFile methods.</description></item>
	/// </list>
	/// </remarks>
	[Obsolete("This class has been split into EncryptedMountConfigurationExtensions and EncryptedJsonFileExtensions. It will be removed in version 3.0.")]
	public static class ConfigurationEncryptedExtension
	{
		/// <inheritdoc cref="EncryptedMountConfigurationExtensions.AddEncryptedMountConfiguration(IConfigurationBuilder, Settings)"/>
		public static IConfigurationBuilder AddEncryptedMountConfiguration(this IConfigurationBuilder configurationBuilder, Settings settings)
		{
			return EncryptedMountConfigurationExtensions.AddEncryptedMountConfiguration(configurationBuilder, settings);
		}

		/// <inheritdoc cref="EncryptedMountConfigurationExtensions.AddEncryptedMountConfiguration(IConfigurationBuilder, string, SettingsProvider, string)"/>
		public static IConfigurationBuilder AddEncryptedMountConfiguration(this IConfigurationBuilder configurationBuilder, string key, SettingsProvider provider, string filename)
		{
			return EncryptedMountConfigurationExtensions.AddEncryptedMountConfiguration(configurationBuilder, key, provider, filename);
		}

		/// <inheritdoc cref="EncryptedMountConfigurationExtensions.AddEncryptedMountConfiguration(IConfigurationBuilder, string, SettingsProvider, string[])"/>
		public static IConfigurationBuilder AddEncryptedMountConfiguration(this IConfigurationBuilder configurationBuilder, string key, SettingsProvider provider, params string[] filenames)
		{
			return EncryptedMountConfigurationExtensions.AddEncryptedMountConfiguration(configurationBuilder, key, provider, filenames);
		}

		/// <inheritdoc cref="EncryptedJsonFileExtensions.AddEncryptedJsonFile(IConfigurationBuilder, Action{EncryptedJsonConfigurationSource})"/>
		public static IConfigurationBuilder AddEncryptedJsonFile(this IConfigurationBuilder builder, Action<EncryptedJsonConfigurationSource> configureSource)
		{
			return EncryptedJsonFileExtensions.AddEncryptedJsonFile(builder, configureSource);
		}

		/// <inheritdoc cref="EncryptedJsonFileExtensions.AddEncryptedJsonFile(IConfigurationBuilder, string, string, bool, bool)"/>
		public static IConfigurationBuilder AddEncryptedJsonFile(this IConfigurationBuilder builder, string path, string key, bool optional, bool reloadOnChange)
		{
			return EncryptedJsonFileExtensions.AddEncryptedJsonFile(builder, path, key, optional, reloadOnChange);
		}

		/// <inheritdoc cref="EncryptedMountConfigurationExtensions.AddEncryptedMountConfiguration(IConfigurationBuilder, Action{Settings})"/>
		public static IConfigurationBuilder AddEncryptedMountConfiguration(this IConfigurationBuilder configurationBuilder, Action<Settings> configureSettings)
		{
			return EncryptedMountConfigurationExtensions.AddEncryptedMountConfiguration(configurationBuilder, configureSettings);
		}

		/// <inheritdoc cref="EncryptedJsonFileExtensions.AddEncryptedJsonFile(IConfigurationBuilder, IFileProvider, string, string, bool, bool)"/>
		public static IConfigurationBuilder AddEncryptedJsonFile(this IConfigurationBuilder builder, IFileProvider provider, string path, string key, bool optional, bool reloadOnChange)
		{
			return EncryptedJsonFileExtensions.AddEncryptedJsonFile(builder, provider, path, key, optional, reloadOnChange);
		}
	}
}
