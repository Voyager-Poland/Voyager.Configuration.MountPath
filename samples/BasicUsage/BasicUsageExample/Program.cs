using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BasicUsageExample;

class Program
{
	static void Main(string[] args)
	{
		Console.WriteLine("=== Voyager.Configuration.MountPath - Basic Usage Example ===\n");

		// Create host with configuration
		var builder = Host.CreateDefaultBuilder(args);

		builder.ConfigureAppConfiguration((context, config) =>
		{
			// Get settings provider from hosting environment
			var provider = context.HostingEnvironment.GetSettingsProvider();

			// Add mount configuration - loads from config/ folder
			// This will load:
			// 1. config/appsettings.json (required)
			// 2. config/appsettings.{Environment}.json (optional)
			config.AddMountConfiguration(provider, "appsettings");

			// You can load multiple configuration files organized by concern
			config.AddMountConfiguration(provider, "database");
			config.AddMountConfiguration(provider, "logging");

			Console.WriteLine($"Environment: {context.HostingEnvironment.EnvironmentName}");
			Console.WriteLine($"Config Path: config/");
			Console.WriteLine();
		});

		var host = builder.Build();

		// Get configuration from DI container
		var configuration = host.Services.GetRequiredService<IConfiguration>();

		// Display loaded configuration values
		DisplayConfiguration(configuration);

		Console.WriteLine("\nPress any key to exit...");
		Console.ReadKey();
	}

	static void DisplayConfiguration(IConfiguration configuration)
	{
		Console.WriteLine("=== Loaded Configuration Values ===\n");

		// Application settings
		Console.WriteLine("Application Settings:");
		Console.WriteLine($"  AppName: {configuration["AppName"]}");
		Console.WriteLine($"  Version: {configuration["Version"]}");
		Console.WriteLine($"  Environment: {configuration["Environment"]}");
		Console.WriteLine();

		// Database configuration
		Console.WriteLine("Database Configuration:");
		var connString = configuration.GetConnectionString("Default");
		Console.WriteLine($"  ConnectionString: {connString}");
		Console.WriteLine($"  Timeout: {configuration["Database:Timeout"]}");
		Console.WriteLine($"  MaxRetries: {configuration["Database:MaxRetries"]}");
		Console.WriteLine();

		// Logging configuration
		Console.WriteLine("Logging Configuration:");
		Console.WriteLine($"  LogLevel:Default: {configuration["Logging:LogLevel:Default"]}");
		Console.WriteLine($"  LogLevel:Microsoft: {configuration["Logging:LogLevel:Microsoft"]}");
		Console.WriteLine();

		// Show all loaded keys
		Console.WriteLine("=== All Configuration Keys ===");
		var allSettings = configuration.AsEnumerable()
			.Where(kv => !string.IsNullOrEmpty(kv.Key))
			.OrderBy(kv => kv.Key);

		foreach (var setting in allSettings)
		{
			Console.WriteLine($"  {setting.Key} = {setting.Value}");
		}
	}
}
