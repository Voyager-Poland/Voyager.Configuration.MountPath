using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Voyager.Configuration.MountPath.Encryption;

namespace Voyager.Configuration.MountPath.Test
{
	[TestFixture]
	public class VconfigCliTest
	{
		private static readonly string ToolProject = Path.GetFullPath(
			Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..",
				"..", "src", "Voyager.Configuration.Tool", "Voyager.Configuration.Tool.csproj"));

		private string _tempDir = null!;

		[SetUp]
		public void SetUp()
		{
			_tempDir = Path.Combine(Path.GetTempPath(), "vconfig-test-" + Guid.NewGuid().ToString("N")[..8]);
			Directory.CreateDirectory(_tempDir);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(_tempDir))
				Directory.Delete(_tempDir, true);
		}

		private (int exitCode, string stdout, string stderr) RunVconfig(string arguments, Dictionary<string, string>? envVars = null)
		{
			var psi = new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = $"run --no-build --project \"{ToolProject}\" -- {arguments}",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			if (envVars != null)
			{
				foreach (var kv in envVars)
					psi.Environment[kv.Key] = kv.Value;
			}

			using var proc = Process.Start(psi)!;
			var stdout = proc.StandardOutput.ReadToEnd();
			var stderr = proc.StandardError.ReadToEnd();
			proc.WaitForExit(60_000);
			return (proc.ExitCode, stdout.Trim(), stderr.Trim());
		}

		[Test]
		public void Keygen_ProducesValid32ByteBase64Key()
		{
			var (exitCode, stdout, _) = RunVconfig("keygen");

			Assert.That(exitCode, Is.EqualTo(0));
			var keyBytes = Convert.FromBase64String(stdout);
			Assert.That(keyBytes, Has.Length.EqualTo(AesGcmCipherProvider.KeySizeBytes));
		}

		[Test]
		public void Keygen_TwoCallsProduceDifferentKeys()
		{
			var (_, key1, _) = RunVconfig("keygen");
			var (_, key2, _) = RunVconfig("keygen");

			Assert.That(key1, Is.Not.EqualTo(key2));
		}

		[Test]
		public void Keygen_StderrContainsSecurityWarning()
		{
			var (_, _, stderr) = RunVconfig("keygen");

			Assert.That(stderr, Does.Contain("ASPNETCORE_ENCODEKEY"));
			Assert.That(stderr, Does.Contain("Anyone with this key"));
		}

		[Test]
		public void Reencrypt_PureDes_MigratesAllValues()
		{
			var desKey = "LegacyDesKey12345678";
			var aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
			var desEncryptor = new Encryptor(desKey);

			var json = new JsonObject
			{
				["connStr"] = desEncryptor.Encrypt("Server=db;Password=secret"),
				["apiKey"] = desEncryptor.Encrypt("my-api-key-123")
			};
			var inputPath = Path.Combine(_tempDir, "secrets.json");
			File.WriteAllText(inputPath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

			var (exitCode, stdout, _) = RunVconfig(
				$"reencrypt --input \"{inputPath}\" --legacy-key-env TEST_DES_KEY --new-key-env TEST_AES_KEY",
				new Dictionary<string, string> { ["TEST_DES_KEY"] = desKey, ["TEST_AES_KEY"] = aesKey });

			Assert.That(exitCode, Is.EqualTo(0), () => stdout);
			Assert.That(stdout, Does.Contain("2 value(s) migrated"));

			var resultJson = JsonNode.Parse(File.ReadAllText(inputPath))!.AsObject();
			Assert.That(resultJson["connStr"]!.GetValue<string>(), Does.StartWith(VersionedEncryptor.V2Prefix));
			Assert.That(resultJson["apiKey"]!.GetValue<string>(), Does.StartWith(VersionedEncryptor.V2Prefix));

			using var reader = new VersionedEncryptor(new AesGcmCipherProvider(aesKey), null, allowLegacyDes: false);
			Assert.That(reader.Decrypt(resultJson["connStr"]!.GetValue<string>()), Is.EqualTo("Server=db;Password=secret"));
			Assert.That(reader.Decrypt(resultJson["apiKey"]!.GetValue<string>()), Is.EqualTo("my-api-key-123"));
		}

		[Test]
		public void Reencrypt_PureAes_IsIdempotent()
		{
			var aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
			using var aesCipher = new AesGcmCipherProvider(aesKey);
			using var writer = new VersionedEncryptor(aesCipher, null, allowLegacyDes: false);

			var v2Val = writer.Encrypt("already-aes");
			var json = new JsonObject { ["val"] = v2Val };
			var inputPath = Path.Combine(_tempDir, "secrets.json");
			File.WriteAllText(inputPath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
			var originalContent = File.ReadAllText(inputPath);

			var (exitCode, stdout, _) = RunVconfig(
				$"reencrypt --input \"{inputPath}\" --legacy-key-env TEST_DES --new-key-env TEST_AES",
				new Dictionary<string, string> { ["TEST_DES"] = "DummyDesKey12345", ["TEST_AES"] = aesKey });

			Assert.That(exitCode, Is.EqualTo(0));
			Assert.That(stdout, Does.Contain("Nothing to migrate"));
			Assert.That(stdout, Does.Contain("1 value(s) already AES"));
			Assert.That(File.ReadAllText(inputPath), Is.EqualTo(originalContent));
		}

		[Test]
		public void Reencrypt_MixedFile_OnlyDesMigrated()
		{
			var desKey = "LegacyDesKey12345678";
			var aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
			var desEncryptor = new Encryptor(desKey);
			using var aesCipher = new AesGcmCipherProvider(aesKey);
			using var aesWriter = new VersionedEncryptor(aesCipher, null, allowLegacyDes: false);

			var json = new JsonObject
			{
				["oldValue"] = desEncryptor.Encrypt("legacy-secret"),
				["newValue"] = aesWriter.Encrypt("modern-secret"),
				["count"] = 42
			};
			var inputPath = Path.Combine(_tempDir, "mixed.json");
			File.WriteAllText(inputPath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

			var (exitCode, stdout, _) = RunVconfig(
				$"reencrypt --input \"{inputPath}\" --legacy-key-env TEST_DES --new-key-env TEST_AES",
				new Dictionary<string, string> { ["TEST_DES"] = desKey, ["TEST_AES"] = aesKey });

			Assert.That(exitCode, Is.EqualTo(0));
			Assert.That(stdout, Does.Contain("1 value(s) migrated"));
			Assert.That(stdout, Does.Contain("1 already AES"));
			Assert.That(stdout, Does.Contain("2 total"));

			var result = JsonNode.Parse(File.ReadAllText(inputPath))!.AsObject();
			Assert.That(result["oldValue"]!.GetValue<string>(), Does.StartWith(VersionedEncryptor.V2Prefix));
			Assert.That(result["newValue"]!.GetValue<string>(), Does.StartWith(VersionedEncryptor.V2Prefix));
			Assert.That(result["count"]!.GetValue<int>(), Is.EqualTo(42));
		}

		[Test]
		public void Reencrypt_DryRun_FileUnchanged()
		{
			var desKey = "LegacyDesKey12345678";
			var aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
			var desEncryptor = new Encryptor(desKey);

			var json = new JsonObject { ["secret"] = desEncryptor.Encrypt("value") };
			var inputPath = Path.Combine(_tempDir, "dryrun.json");
			File.WriteAllText(inputPath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
			var originalContent = File.ReadAllText(inputPath);

			var (exitCode, stdout, _) = RunVconfig(
				$"reencrypt --input \"{inputPath}\" --legacy-key-env TEST_DES --new-key-env TEST_AES --dry-run",
				new Dictionary<string, string> { ["TEST_DES"] = desKey, ["TEST_AES"] = aesKey });

			Assert.That(exitCode, Is.EqualTo(0));
			Assert.That(stdout, Does.Contain("Dry run"));
			Assert.That(stdout, Does.Contain("1 value(s) would be migrated"));
			Assert.That(File.ReadAllText(inputPath), Is.EqualTo(originalContent));
		}
	}
}
