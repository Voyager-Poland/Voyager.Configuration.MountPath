﻿using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Voyager.Configuration.MountPath;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class ConfigurationExtension
	{
		public static IConfigurationBuilder AddMountConfiguration(this IConfigurationBuilder configurationBuilder, SettingsProvider provider, string filename = "appsettings")
		{
			var setting = provider.GetSettings(filename);
			return AddMountConfiguration(configurationBuilder, setting);
		}

		public static IConfigurationBuilder AddMountConfiguration(this IConfigurationBuilder configurationBuilder, SettingsProvider provider, params string[] filenames)
		{
			foreach (var filename in filenames)
			{
				var setting = provider.GetSettings(filename);
				AddMountConfiguration(configurationBuilder, setting);
			}
			return configurationBuilder;
		}

		public static IConfigurationBuilder AddMountConfiguration(this IConfigurationBuilder configurationBuilder, Action<Settings> actDeleg)
		{
			var setting = SettingsProvider.PrepareDefault();
			actDeleg.Invoke(setting);
			return AddMountConfiguration(configurationBuilder, setting);
		}


		public static IConfigurationBuilder AddMountConfiguration(this IConfigurationBuilder configurationBuilder, Settings settings)
		{
			configurationBuilder.SetBasePath(Path.Combine(settings.CurrentDirectory, settings.ConfigMountPath))
											.AddJsonFile($"{settings.FileName}.json", optional: false, reloadOnChange: true)
											.AddJsonFile($"{settings.FileName}.{settings.HostingName}.json", optional: settings.Optional, reloadOnChange: true);
			return configurationBuilder;
		}
	}
}
