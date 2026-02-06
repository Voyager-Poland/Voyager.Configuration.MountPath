using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;
using Voyager.Configuration.MountPath.Encryption;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Extension methods for adding encrypted JSON configuration files.
	/// Provides low-level API for adding individual encrypted JSON files to configuration.
	/// </summary>
	/// <remarks>
	/// This class follows the Single Responsibility Principle by handling only JSON file operations.
	/// For high-level mount path configuration, use <see cref="EncryptedMountConfigurationExtensions"/>.
	/// </remarks>
	public static class EncryptedJsonFileExtensions
	{
		/// <summary>
		/// Adds an encrypted JSON configuration source to the builder.
		/// </summary>
		/// <param name="builder">The configuration builder.</param>
		/// <param name="configureSource">Action to configure the encrypted JSON configuration source.</param>
		/// <returns>The configuration builder for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when builder or configureSource is null.</exception>
		public static IConfigurationBuilder AddEncryptedJsonFile(this IConfigurationBuilder builder, Action<EncryptedJsonConfigurationSource> configureSource)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));
			if (configureSource == null)
				throw new ArgumentNullException(nameof(configureSource));

			var source = new EncryptedJsonConfigurationSource();
			configureSource(source);
			return builder.Add(source);
		}

		/// <summary>
		/// Adds an encrypted JSON configuration file to the builder.
		/// </summary>
		/// <param name="builder">The configuration builder.</param>
		/// <param name="path">The path to the JSON file.</param>
		/// <param name="key">The encryption key.</param>
		/// <param name="optional">Whether the file is optional.</param>
		/// <param name="reloadOnChange">Whether to reload configuration when the file changes.</param>
		/// <returns>The configuration builder for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when builder or path is null.</exception>
		public static IConfigurationBuilder AddEncryptedJsonFile(this IConfigurationBuilder builder, string path, string key, bool optional, bool reloadOnChange)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			return AddEncryptedJsonFile(builder, provider: null, path: path, key: key, optional: optional, reloadOnChange: reloadOnChange);
		}

		/// <summary>
		/// Adds an encrypted JSON configuration file to the builder with a custom file provider.
		/// </summary>
		/// <param name="builder">The configuration builder.</param>
		/// <param name="provider">The file provider to use to access the configuration file. If null, the default provider is used.</param>
		/// <param name="path">The path to the JSON file.</param>
		/// <param name="key">The encryption key.</param>
		/// <param name="optional">Whether the file is optional.</param>
		/// <param name="reloadOnChange">Whether to reload configuration when the file changes.</param>
		/// <returns>The configuration builder for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
		/// <exception cref="ArgumentException">Thrown when path is null or empty.</exception>
		public static IConfigurationBuilder AddEncryptedJsonFile(this IConfigurationBuilder builder, IFileProvider? provider, string path, string key, bool optional, bool reloadOnChange)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));
			if (string.IsNullOrEmpty(path))
				throw new ArgumentException("Path cannot be null or empty.", nameof(path));

			return AddEncryptedJsonFile(builder, s =>
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
