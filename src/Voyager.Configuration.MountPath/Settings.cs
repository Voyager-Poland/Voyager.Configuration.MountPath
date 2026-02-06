using System;
using System.IO;

namespace Voyager.Configuration.MountPath
{
	/// <summary>
	/// Configuration settings for mount path-based configuration loading.
	/// </summary>
	public class Settings
	{
		private string _currentDirectory;
		private string _fileName;
		private string _configMountPath;
		private string _hostingName;
		private string? _key;

		/// <summary>
		/// Initializes a new instance of the <see cref="Settings"/> class with default values.
		/// </summary>
		public Settings()
		{
			_fileName = SettingsDefaults.DefaultFileName;
			_configMountPath = SettingsDefaults.DefaultConfigMountPath;
			_hostingName = SettingsDefaults.DefaultHostingName;
			_currentDirectory = Directory.GetCurrentDirectory();
			Optional = true;
		}

		/// <summary>
		/// Gets or sets the current working directory for configuration files.
		/// </summary>
		/// <exception cref="ArgumentException">Thrown when value is null or whitespace.</exception>
		public string CurrentDirectory
		{
			get => _currentDirectory;
			set
			{
				if (string.IsNullOrWhiteSpace(value))
					throw new ArgumentException("Current directory cannot be null or whitespace.", nameof(CurrentDirectory));
				_currentDirectory = value;
			}
		}

		/// <summary>
		/// Gets or sets the configuration file name (without extension).
		/// </summary>
		/// <exception cref="ArgumentException">Thrown when value is null or whitespace.</exception>
		public string FileName
		{
			get => _fileName;
			set
			{
				if (string.IsNullOrWhiteSpace(value))
					throw new ArgumentException("File name cannot be null or whitespace.", nameof(FileName));
				_fileName = value;
			}
		}

		/// <summary>
		/// Gets or sets the mount path where configuration files are located.
		/// </summary>
		/// <exception cref="ArgumentException">Thrown when value is null or whitespace.</exception>
		public string ConfigMountPath
		{
			get => _configMountPath;
			set
			{
				if (string.IsNullOrWhiteSpace(value))
					throw new ArgumentException("Config mount path cannot be null or whitespace.", nameof(ConfigMountPath));
				_configMountPath = value;
			}
		}

		/// <summary>
		/// Gets or sets the hosting environment name (e.g., Development, Production).
		/// </summary>
		/// <exception cref="ArgumentException">Thrown when value is null or whitespace.</exception>
		public string HostingName
		{
			get => _hostingName;
			set
			{
				if (string.IsNullOrWhiteSpace(value))
					throw new ArgumentException("Hosting name cannot be null or whitespace.", nameof(HostingName));
				_hostingName = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the configuration file is optional.
		/// </summary>
		public bool Optional { get; set; }

		/// <summary>
		/// Gets or sets the encryption key for encrypted configuration values.
		/// Null if encryption is not used.
		/// </summary>
		/// <exception cref="ArgumentException">Thrown when key is not null but shorter than minimum required length.</exception>
		public string? Key
		{
			get => _key;
			set
			{
				if (value != null && value.Length < SettingsDefaults.MinimumKeyLength)
					throw new ArgumentException($"Encryption key must be at least {SettingsDefaults.MinimumKeyLength} characters long.", nameof(Key));
				_key = value;
			}
		}
	}
}
