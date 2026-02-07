using Microsoft.Extensions.Configuration.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace Voyager.Configuration.MountPath.Encryption
{
	/// <summary>
	/// Configuration provider that decrypts encrypted JSON configuration values.
	/// </summary>
	public class EncryptedJsonConfigurationProvider : JsonConfigurationProvider
	{
		private readonly IEncryptor encryptor;
		private readonly EncryptedJsonConfigurationSource _source;

		/// <summary>
		/// Initializes a new instance of the <see cref="EncryptedJsonConfigurationProvider"/> class.
		/// </summary>
		/// <param name="source">The configuration source.</param>
		/// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when no key is provided.</exception>
		public EncryptedJsonConfigurationProvider(EncryptedJsonConfigurationSource source) : base(source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (string.IsNullOrEmpty(source.Key))
				throw new InvalidOperationException("Encryption key must be provided.");

			_source = source;
			var factory = source.EncryptorFactory ?? new DefaultEncryptorFactory();
			encryptor = factory.Create(source.Key);
		}

		/// <summary>
		/// Loads configuration from the source with enhanced error handling.
		/// </summary>
		/// <exception cref="ConfigurationException">Thrown when configuration loading fails.</exception>
		/// <exception cref="EncryptionException">Thrown when decryption fails.</exception>
		public override void Load()
		{
			try
			{
				base.Load();
			}
			catch (FileNotFoundException ex)
			{
				throw new ConfigurationException(
					$"Failed to load configuration file '{Path.GetFileName(_source.Path)}'. " +
					$"File not found: {_source.Path}",
					Path.GetDirectoryName(_source.Path),
					Path.GetFileName(_source.Path),
					ex);
			}
			catch (JsonException ex)
			{
				var lineInfo = ex.LineNumber.HasValue ? $" at line {ex.LineNumber}" : "";
				throw new ConfigurationException(
					$"Failed to parse configuration file '{Path.GetFileName(_source.Path)}'. " +
					$"The file contains invalid JSON{lineInfo}. Check file syntax.",
					Path.GetDirectoryName(_source.Path),
					Path.GetFileName(_source.Path),
					ex);
			}
			catch (Exception ex) when (ex is not ConfigurationException && ex is not EncryptionException)
			{
				// Wrap any other unexpected exceptions
				throw new ConfigurationException(
					$"Unexpected error loading configuration file '{Path.GetFileName(_source.Path)}': {ex.Message}",
					Path.GetDirectoryName(_source.Path),
					Path.GetFileName(_source.Path),
					ex);
			}
		}

		/// <inheritdoc />
		public override void Load(Stream stream)
		{
			base.Load(stream);

			// Decrypt all configuration values
			try
			{
				foreach (string key in Data.Keys)
				{
					var value = Data[key];
					if (value != null)
					{
						try
						{
							Data[key] = encryptor.Decrypt(value);
						}
						catch (CryptographicException ex)
						{
							throw new EncryptionException(
								$"Failed to decrypt configuration value in '{Path.GetFileName(_source.Path)}'. " +
								$"Key: '{key}'. " +
								$"Ensure the encryption key is correct and the value was encrypted with compatible settings.",
								Path.GetDirectoryName(_source.Path),
								Path.GetFileName(_source.Path),
								key,
								ex);
						}
						catch (FormatException ex)
						{
							throw new EncryptionException(
								$"Failed to decrypt configuration value in '{Path.GetFileName(_source.Path)}'. " +
								$"Key: '{key}'. " +
								$"The value is not in a valid encrypted format.",
								Path.GetDirectoryName(_source.Path),
								Path.GetFileName(_source.Path),
								key,
								ex);
						}
					}
				}
			}
			catch (Exception ex) when (ex is not EncryptionException)
			{
				// Wrap any other decryption errors
				throw new EncryptionException(
					$"Failed to decrypt configuration values in '{Path.GetFileName(_source.Path)}'. " +
					$"Ensure the encryption key is correct.",
					Path.GetDirectoryName(_source.Path),
					Path.GetFileName(_source.Path),
					null,
					ex);
			}
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (encryptor is IDisposable disposableEncryptor)
				{
					disposableEncryptor.Dispose();
				}
			}
			base.Dispose(disposing);
		}
	}
}
