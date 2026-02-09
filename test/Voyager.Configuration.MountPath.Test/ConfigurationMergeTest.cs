using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace Voyager.Configuration.MountPath.Test
{
	/// <summary>
	/// Tests for configuration merge and override scenarios.
	/// </summary>
	[TestFixture]
	public class ConfigurationMergeTest : ConfigurationTestBase
	{
		private string _testConfigPath = null!;

		[TearDown]
		public new void TearDown()
		{
			if (Directory.Exists(_testConfigPath))
			{
				Directory.Delete(_testConfigPath, true);
			}
		}

		protected override void ConfigureHost(HostBuilderContext context, IConfigurationBuilder config)
		{
			// Initialize test config path here (called before base.SetUp completes)
			_testConfigPath = Path.Combine(Path.GetTempPath(), "config-merge-test", Guid.NewGuid().ToString());
			Directory.CreateDirectory(_testConfigPath);

			// Create test configuration files
			var baseConfig = Path.Combine(_testConfigPath, "appsettings.json");
			var envConfig = Path.Combine(_testConfigPath, $"appsettings.{context.HostingEnvironment.EnvironmentName}.json");

			File.WriteAllText(baseConfig, @"{
				""BaseValue"": ""from-base"",
				""OverriddenValue"": ""base-original"",
				""NestedConfig"": {
					""Level1"": ""base-level1"",
					""Level2"": ""base-level2""
				},
				""ConnectionStrings"": {
					""Default"": ""base-connection""
				}
			}");

			File.WriteAllText(envConfig, @"{
				""OverriddenValue"": ""env-override"",
				""EnvValue"": ""from-environment"",
				""NestedConfig"": {
					""Level1"": ""env-level1""
				},
				""ConnectionStrings"": {
					""Default"": ""env-connection"",
					""Secondary"": ""env-secondary""
				}
			}");

			config.SetBasePath(_testConfigPath)
				  .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
				  .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false);
		}

		[Test]
		public void Configuration_BaseValueIsPreserved()
		{
			Assert.That(Configuration["BaseValue"], Is.EqualTo("from-base"));
		}

		[Test]
		public void Configuration_EnvironmentValueOverridesBase()
		{
			Assert.That(Configuration["OverriddenValue"], Is.EqualTo("env-override"));
		}

		[Test]
		public void Configuration_EnvironmentOnlyValueIsAvailable()
		{
			Assert.That(Configuration["EnvValue"], Is.EqualTo("from-environment"));
		}

		[Test]
		public void Configuration_NestedValueIsOverridden()
		{
			Assert.That(Configuration["NestedConfig:Level1"], Is.EqualTo("env-level1"));
		}

		[Test]
		public void Configuration_NestedValueNotOverriddenIsPreserved()
		{
			Assert.That(Configuration["NestedConfig:Level2"], Is.EqualTo("base-level2"));
		}

		[Test]
		public void Configuration_ConnectionStringIsOverridden()
		{
			Assert.That(Configuration.GetConnectionString("Default"), Is.EqualTo("env-connection"));
		}

		[Test]
		public void Configuration_NewConnectionStringIsAvailable()
		{
			Assert.That(Configuration.GetConnectionString("Secondary"), Is.EqualTo("env-secondary"));
		}

		[Test]
		public void Configuration_MultipleFilesInOrder()
		{
			// This test verifies that later files override earlier files
			var allValues = Configuration.AsEnumerable().ToDictionary(k => k.Key, v => v.Value);

			Assert.That(allValues["OverriddenValue"], Is.EqualTo("env-override"),
				"Environment config should override base config");
		}
	}

	/// <summary>
	/// Tests for multiple configuration files with different names.
	/// </summary>
	[TestFixture]
	public class MultipleConfigurationFilesTest
	{
		private string _testConfigPath = null!;
		private IConfiguration _configuration = null!;

		[SetUp]
		public void SetUp()
		{
			_testConfigPath = Path.Combine(Path.GetTempPath(), "multi-config-test", Guid.NewGuid().ToString());
			Directory.CreateDirectory(_testConfigPath);

			// Create multiple config files
			File.WriteAllText(Path.Combine(_testConfigPath, "database.json"), @"{
				""ConnectionStrings"": {
					""Database"": ""db-connection""
				}
			}");

			File.WriteAllText(Path.Combine(_testConfigPath, "logging.json"), @"{
				""Logging"": {
					""LogLevel"": {
						""Default"": ""Information""
					}
				}
			}");

			File.WriteAllText(Path.Combine(_testConfigPath, "services.json"), @"{
				""Services"": {
					""ApiUrl"": ""https://api.example.com""
				}
			}");

			var builder = new ConfigurationBuilder()
				.SetBasePath(_testConfigPath)
				.AddJsonFile("database.json", optional: false)
				.AddJsonFile("logging.json", optional: false)
				.AddJsonFile("services.json", optional: false);

			_configuration = builder.Build();
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(_testConfigPath))
			{
				Directory.Delete(_testConfigPath, true);
			}
		}

		[Test]
		public void MultipleFiles_AllValuesAreAccessible()
		{
			Assert.That(_configuration.GetConnectionString("Database"), Is.EqualTo("db-connection"));
			Assert.That(_configuration["Logging:LogLevel:Default"], Is.EqualTo("Information"));
			Assert.That(_configuration["Services:ApiUrl"], Is.EqualTo("https://api.example.com"));
		}

		[Test]
		public void MultipleFiles_EachFileContainsIsolatedConfiguration()
		{
			// Verify that values from different files don't interfere with each other
			var allKeys = _configuration.AsEnumerable().Select(kv => kv.Key).ToList();

			Assert.That(allKeys, Contains.Item("ConnectionStrings:Database"));
			Assert.That(allKeys, Contains.Item("Logging:LogLevel:Default"));
			Assert.That(allKeys, Contains.Item("Services:ApiUrl"));
		}
	}
}
