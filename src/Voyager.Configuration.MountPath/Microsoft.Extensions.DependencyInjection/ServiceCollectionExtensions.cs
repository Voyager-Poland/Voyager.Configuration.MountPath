using System;
using Voyager.Configuration.MountPath;
using Voyager.Configuration.MountPath.Encryption;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Extension methods for registering Voyager.Configuration.MountPath services in DI container.
	/// </summary>
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Adds the default encryptor factory to the service collection.
		/// </summary>
		/// <param name="services">The service collection.</param>
		/// <returns>The service collection for chaining.</returns>
		public static IServiceCollection AddVoyagerEncryption(this IServiceCollection services)
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));

			services.AddSingleton<IEncryptorFactory, DefaultEncryptorFactory>();
			return services;
		}

		/// <summary>
		/// Adds a custom encryptor factory to the service collection.
		/// </summary>
		/// <typeparam name="TFactory">The type of the encryptor factory.</typeparam>
		/// <param name="services">The service collection.</param>
		/// <returns>The service collection for chaining.</returns>
		public static IServiceCollection AddVoyagerEncryption<TFactory>(this IServiceCollection services)
			where TFactory : class, IEncryptorFactory
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));

			services.AddSingleton<IEncryptorFactory, TFactory>();
			return services;
		}

		/// <summary>
		/// Adds a custom encryptor factory instance to the service collection.
		/// </summary>
		/// <param name="services">The service collection.</param>
		/// <param name="factory">The encryptor factory instance.</param>
		/// <returns>The service collection for chaining.</returns>
		public static IServiceCollection AddVoyagerEncryption(this IServiceCollection services, IEncryptorFactory factory)
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));

			services.AddSingleton(factory);
			return services;
		}

		/// <summary>
		/// Adds the default settings provider to the service collection.
		/// </summary>
		/// <param name="services">The service collection.</param>
		/// <returns>The service collection for chaining.</returns>
		public static IServiceCollection AddVoyagerConfiguration(this IServiceCollection services)
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));

			services.AddSingleton<ISettingsProvider, SettingsProvider>();
			return services;
		}

		/// <summary>
		/// Adds a custom settings provider to the service collection.
		/// </summary>
		/// <typeparam name="TProvider">The type of the settings provider.</typeparam>
		/// <param name="services">The service collection.</param>
		/// <returns>The service collection for chaining.</returns>
		public static IServiceCollection AddVoyagerConfiguration<TProvider>(this IServiceCollection services)
			where TProvider : class, ISettingsProvider
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));

			services.AddSingleton<ISettingsProvider, TProvider>();
			return services;
		}
	}
}
