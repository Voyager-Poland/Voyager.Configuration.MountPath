using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using Voyager.Configuration.MountPath;
using Voyager.Configuration.MountPath.Encryption;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class ConfigurationEncryptedExtension
	{

		public static IConfigurationBuilder AddEncryptedMountConfiguration(this IConfigurationBuilder configurationBuilder, Settings settings)
		{
			configurationBuilder.SetBasePath(Path.Combine(settings.CurrentDirectory, settings.ConfigMountPath))
											.AddEncryptedJsonFile($"{settings.FileName}.json", settings.Key, optional: false, reloadOnChange: true)
											.AddEncryptedJsonFile($"{settings.FileName}.{settings.HostingName}.json", settings.Key, optional: settings.Optional, reloadOnChange: true);
			return configurationBuilder;
		}


		public static IConfigurationBuilder AddEncryptedMountConfiguration(this IConfigurationBuilder configurationBuilder, string key, SettingsProvider provider, string filename)
		{
			var setting = provider.GetSettings(filename);
			setting.Key = key;
			return AddEncryptedMountConfiguration(configurationBuilder, setting);
		}


		public static IConfigurationBuilder AddEncryptedMountConfiguration(this IConfigurationBuilder configurationBuilder, string key, SettingsProvider provider, params string[] filenames)
		{
			foreach (var filename in filenames)
			{
				var setting = provider.GetSettings(filename);
				setting.Key = key;
				AddEncryptedMountConfiguration(configurationBuilder, setting);
			}
			return configurationBuilder;
		}




		public static IConfigurationBuilder AddEncryptedJsonFile(this IConfigurationBuilder builder, Action<EncryptedJsonConfigurationSource> configureSource)
		=> builder.Add(configureSource);


		public static IConfigurationBuilder AddEncryptedJsonFile(this IConfigurationBuilder builder, string path, string key, bool optional, bool reloadOnChange)
		{
			return AddEncryptedJsonFile(builder, provider: null, path: path, key: key, optional: optional, reloadOnChange: reloadOnChange);
		}


		public static IConfigurationBuilder AddEncryptedJsonFile(this IConfigurationBuilder builder, IFileProvider provider, string path, string key, bool optional, bool reloadOnChange)
		{
			if (builder == null)
			{
				throw new ArgumentNullException(nameof(builder));
			}
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentException("Wrong path");
			}

			return builder.AddEncryptedJsonFile(s =>
			{
				s.FileProvider = provider;
				s.Path = path;
				s.Optional = optional;
				s.ReloadOnChange = reloadOnChange;
				s.Key = key;
				s.ResolveFileProvider();
			});
		}



	}
}
