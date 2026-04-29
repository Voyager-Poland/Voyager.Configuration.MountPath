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

		private static readonly string Configuration =
			new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent!.Name;

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
				Arguments = $"run --no-build -c {Configuration} --project \"{ToolProject}\" -- {arguments}",
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
		public void Reencrypt_Base64LookingPlaintext_LeftUntouched()
		{
			var desKey = "LegacyDesKey12345678";
			var aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
			var desEncryptor = new Encryptor(desKey);

			// Encrypt "test" with a DIFFERENT DES key → valid Base64, valid DES structure,
			// but decrypted with our key it produces garbage or throws.
			var otherDesEncryptor = new Encryptor("OtherDesKey12345");
			var wrongKeyCiphertext = otherDesEncryptor.Encrypt("test-value");

			var json = new JsonObject
			{
				["secret"] = desEncryptor.Encrypt("real-secret"),
				["base64Token"] = "SGVsbG8gV29ybGQ=",
				["wrongKeyCipher"] = wrongKeyCiphertext,
				["normalText"] = "just plain text"
			};
			var inputPath = Path.Combine(_tempDir, "base64-plaintext.json");
			File.WriteAllText(inputPath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

			var (exitCode, stdout, _) = RunVconfig(
				$"reencrypt --input \"{inputPath}\" --legacy-key-env TEST_DES --new-key-env TEST_AES",
				new Dictionary<string, string> { ["TEST_DES"] = desKey, ["TEST_AES"] = aesKey });

			Assert.That(exitCode, Is.EqualTo(0));

			var result = JsonNode.Parse(File.ReadAllText(inputPath))!.AsObject();
			Assert.That(result["secret"]!.GetValue<string>(), Does.StartWith(VersionedEncryptor.V2Prefix),
				"Real DES value should be migrated");
			Assert.That(result["base64Token"]!.GetValue<string>(), Is.EqualTo("SGVsbG8gV29ybGQ="),
				"Base64-looking plaintext must not be corrupted");
			Assert.That(result["wrongKeyCipher"]!.GetValue<string>(), Is.EqualTo(wrongKeyCiphertext),
				"DES ciphertext from wrong key must not be corrupted");
			Assert.That(result["normalText"]!.GetValue<string>(), Is.EqualTo("just plain text"),
				"Plain text must not be corrupted");
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
		public void Reencrypt_MixedWithPlaintext_PlaintextLeftUntouched()
		{
			var desKey = "LegacyDesKey12345678";
			var aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
			var desEncryptor = new Encryptor(desKey);
			using var aesCipher = new AesGcmCipherProvider(aesKey);
			using var aesWriter = new VersionedEncryptor(aesCipher, null, allowLegacyDes: false);

			var json = new JsonObject
			{
				["host"] = "localhost",
				["port"] = 5432,
				["password"] = desEncryptor.Encrypt("db-secret"),
				["apiKey"] = aesWriter.Encrypt("modern-key"),
				["description"] = "Production database",
				["enabled"] = true
			};
			var inputPath = Path.Combine(_tempDir, "mixed-plaintext.json");
			File.WriteAllText(inputPath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

			var (exitCode, stdout, _) = RunVconfig(
				$"reencrypt --input \"{inputPath}\" --legacy-key-env TEST_DES --new-key-env TEST_AES",
				new Dictionary<string, string> { ["TEST_DES"] = desKey, ["TEST_AES"] = aesKey });

			Assert.That(exitCode, Is.EqualTo(0));
			Assert.That(stdout, Does.Contain("1 value(s) migrated"));

			var result = JsonNode.Parse(File.ReadAllText(inputPath))!.AsObject();
			Assert.That(result["host"]!.GetValue<string>(), Is.EqualTo("localhost"));
			Assert.That(result["port"]!.GetValue<int>(), Is.EqualTo(5432));
			Assert.That(result["password"]!.GetValue<string>(), Does.StartWith(VersionedEncryptor.V2Prefix));
			Assert.That(result["apiKey"]!.GetValue<string>(), Does.StartWith(VersionedEncryptor.V2Prefix));
			Assert.That(result["description"]!.GetValue<string>(), Is.EqualTo("Production database"));
			Assert.That(result["enabled"]!.GetValue<bool>(), Is.True);

			using var reader = new VersionedEncryptor(new AesGcmCipherProvider(aesKey), null, allowLegacyDes: false);
			Assert.That(reader.Decrypt(result["password"]!.GetValue<string>()), Is.EqualTo("db-secret"));
		}

		[Test]
		public void Decrypt_JsonWithComments_SkipsCommentsAndDecrypts()
		{
			var aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
			using var aesCipher = new AesGcmCipherProvider(aesKey);
			using var writer = new VersionedEncryptor(aesCipher, legacyDes: null, allowLegacyDes: false);
			var encryptedSecret = writer.Encrypt("plaintext-secret");

			// ASP.NET Core-style JSONC: line and block comments mixed in.
			// Reproduces the bug where JsonNode.Parse threw on '/' at start of property name.
			var jsonc = "{\n" +
				"  // Connection string for production database\n" +
				$"  \"ConnectionString\": \"{encryptedSecret}\",\n" +
				"  /* Timeout in seconds */\n" +
				"  \"Timeout\": 30\n" +
				"}\n";
			var inputPath = Path.Combine(_tempDir, "config.json");
			var outputPath = Path.Combine(_tempDir, "config.plain.json");
			File.WriteAllText(inputPath, jsonc);

			var (exitCode, _, stderr) = RunVconfig(
				$"decrypt --input \"{inputPath}\" --output \"{outputPath}\" --key-env TEST_KEY",
				new Dictionary<string, string> { ["TEST_KEY"] = aesKey });

			Assert.That(exitCode, Is.EqualTo(0), () => stderr);

			var result = JsonNode.Parse(File.ReadAllText(outputPath))!.AsObject();
			Assert.That(result["ConnectionString"]!.GetValue<string>(), Is.EqualTo("plaintext-secret"));
			Assert.That(result["Timeout"]!.GetValue<int>(), Is.EqualTo(30));
		}

		[Test]
		public void Encrypt_JsonWithTrailingComma_Succeeds()
		{
			var aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

			var jsonWithTrailingComma = "{\n" +
				"  \"ApiKey\": \"secret-value\",\n" +
				"  \"Timeout\": 30,\n" +
				"}\n";
			var inputPath = Path.Combine(_tempDir, "config.json");
			var outputPath = Path.Combine(_tempDir, "config.encrypted.json");
			File.WriteAllText(inputPath, jsonWithTrailingComma);

			var (exitCode, _, stderr) = RunVconfig(
				$"encrypt --input \"{inputPath}\" --output \"{outputPath}\" --key-env TEST_KEY",
				new Dictionary<string, string> { ["TEST_KEY"] = aesKey });

			Assert.That(exitCode, Is.EqualTo(0), () => stderr);

			// Round-trip: decrypt the v2: encrypted output with AES to confirm the encrypt path worked.
			using var aesCipher = new AesGcmCipherProvider(aesKey);
			using var reader = new VersionedEncryptor(aesCipher, legacyDes: null, allowLegacyDes: false);
			var result = JsonNode.Parse(File.ReadAllText(outputPath))!.AsObject();
			Assert.That(result["ApiKey"]!.GetValue<string>(), Does.StartWith(VersionedEncryptor.V2Prefix));
			Assert.That(reader.Decrypt(result["ApiKey"]!.GetValue<string>()), Is.EqualTo("secret-value"));
			Assert.That(result["Timeout"]!.GetValue<int>(), Is.EqualTo(30));
		}

		[Test]
		public void EncryptValue_WritesV2Prefix()
		{
			var aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

			var (exitCode, stdout, stderr) = RunVconfig(
				$"encrypt-value \"my-secret\" --key-env TEST_KEY",
				new Dictionary<string, string> { ["TEST_KEY"] = aesKey });

			Assert.That(exitCode, Is.EqualTo(0), () => stderr);
			Assert.That(stdout, Does.StartWith(VersionedEncryptor.V2Prefix));

			// Round-trip via library
			using var aesCipher = new AesGcmCipherProvider(aesKey);
			using var reader = new VersionedEncryptor(aesCipher, legacyDes: null, allowLegacyDes: false);
			Assert.That(reader.Decrypt(stdout), Is.EqualTo("my-secret"));
		}

		[Test]
		public void DecryptValue_WithLegacyKeyEnv_ReadsDesCiphertext()
		{
			var aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
			var desKey = "LegacyDesKey12345678";
			var desCiphertext = new Encryptor(desKey).Encrypt("legacy-secret");

			var (exitCode, stdout, stderr) = RunVconfig(
				$"decrypt-value \"{desCiphertext}\" --key-env TEST_AES --legacy-key-env TEST_DES",
				new Dictionary<string, string> { ["TEST_AES"] = aesKey, ["TEST_DES"] = desKey });

			Assert.That(exitCode, Is.EqualTo(0), () => stderr);
			Assert.That(stdout, Is.EqualTo("legacy-secret"));
		}

		[Test]
		public void Decrypt_JsonWithMixedV2AndDes_WithLegacyKey_DecryptsBoth()
		{
			var aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
			var desKey = "LegacyDesKey12345678";

			using var aesCipher = new AesGcmCipherProvider(aesKey);
			using var aesWriter = new VersionedEncryptor(aesCipher, legacyDes: null, allowLegacyDes: false);
			var desEncryptor = new Encryptor(desKey);

			// Strict-decrypt: every string must be ciphertext. Numbers are left alone.
			var json = new JsonObject
			{
				["modernSecret"] = aesWriter.Encrypt("modern-value"),
				["legacySecret"] = desEncryptor.Encrypt("legacy-value"),
				["count"] = 42
			};
			var inputPath = Path.Combine(_tempDir, "mixed.json");
			var outputPath = Path.Combine(_tempDir, "mixed.plain.json");
			File.WriteAllText(inputPath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

			var (exitCode, _, stderr) = RunVconfig(
				$"decrypt --input \"{inputPath}\" --output \"{outputPath}\" --key-env TEST_AES --legacy-key-env TEST_DES",
				new Dictionary<string, string> { ["TEST_AES"] = aesKey, ["TEST_DES"] = desKey });

			Assert.That(exitCode, Is.EqualTo(0), () => stderr);

			var result = JsonNode.Parse(File.ReadAllText(outputPath))!.AsObject();
			Assert.That(result["modernSecret"]!.GetValue<string>(), Is.EqualTo("modern-value"));
			Assert.That(result["legacySecret"]!.GetValue<string>(), Is.EqualTo("legacy-value"));
			Assert.That(result["count"]!.GetValue<int>(), Is.EqualTo(42));
		}

		[Test]
		public void Encrypt_Then_Decrypt_RoundTrip_RestoresOriginalValues()
		{
			// Full CLI round-trip: vconfig encrypt → vconfig decrypt with the same key.
			// This is the most basic invariant — if it doesn't hold, the tool is broken.
			var aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

			var originalJson =
				"{\n" +
				"  \"ConnectionString\": \"Server=db;Password=secret\",\n" +
				"  \"ApiKey\": \"my-api-key with spaces and stuff\",\n" +
				"  \"Nested\": { \"InnerSecret\": \"deep-value\" },\n" +
				"  \"Timeout\": 30,\n" +
				"  \"Enabled\": true\n" +
				"}\n";
			var inputPath = Path.Combine(_tempDir, "config.json");
			var encryptedPath = Path.Combine(_tempDir, "config.encrypted.json");
			var decryptedPath = Path.Combine(_tempDir, "config.decrypted.json");
			File.WriteAllText(inputPath, originalJson);

			var (encExit, _, encErr) = RunVconfig(
				$"encrypt --input \"{inputPath}\" --output \"{encryptedPath}\" --key-env TEST_KEY",
				new Dictionary<string, string> { ["TEST_KEY"] = aesKey });
			Assert.That(encExit, Is.EqualTo(0), () => encErr);

			// String values are now ciphertext (v2: prefix); non-strings unchanged.
			var encrypted = JsonNode.Parse(File.ReadAllText(encryptedPath))!.AsObject();
			Assert.That(encrypted["ConnectionString"]!.GetValue<string>(),
				Does.StartWith(VersionedEncryptor.V2Prefix));
			Assert.That(encrypted["ApiKey"]!.GetValue<string>(),
				Does.StartWith(VersionedEncryptor.V2Prefix));
			Assert.That(encrypted["Nested"]!["InnerSecret"]!.GetValue<string>(),
				Does.StartWith(VersionedEncryptor.V2Prefix));
			Assert.That(encrypted["Timeout"]!.GetValue<int>(), Is.EqualTo(30));
			Assert.That(encrypted["Enabled"]!.GetValue<bool>(), Is.True);

			var (decExit, _, decErr) = RunVconfig(
				$"decrypt --input \"{encryptedPath}\" --output \"{decryptedPath}\" --key-env TEST_KEY",
				new Dictionary<string, string> { ["TEST_KEY"] = aesKey });
			Assert.That(decExit, Is.EqualTo(0), () => decErr);

			var decrypted = JsonNode.Parse(File.ReadAllText(decryptedPath))!.AsObject();
			Assert.That(decrypted["ConnectionString"]!.GetValue<string>(),
				Is.EqualTo("Server=db;Password=secret"));
			Assert.That(decrypted["ApiKey"]!.GetValue<string>(),
				Is.EqualTo("my-api-key with spaces and stuff"));
			Assert.That(decrypted["Nested"]!["InnerSecret"]!.GetValue<string>(),
				Is.EqualTo("deep-value"));
			Assert.That(decrypted["Timeout"]!.GetValue<int>(), Is.EqualTo(30));
			Assert.That(decrypted["Enabled"]!.GetValue<bool>(), Is.True);
		}

		[Test]
		public void Decrypt_NonEncryptedValue_FailsLoudWithJsonPath()
		{
			// Strict mode: a string that isn't valid ciphertext must error out (no silent passthrough).
			// The error must include the JSON path so the user knows which value broke.
			var aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

			var plaintextJson =
				"{\n" +
				"  \"Database\": {\n" +
				"    \"ConnectionString\": \"Server=localhost\"\n" +
				"  }\n" +
				"}\n";
			var inputPath = Path.Combine(_tempDir, "plain.json");
			var outputPath = Path.Combine(_tempDir, "out.json");
			File.WriteAllText(inputPath, plaintextJson);

			var (exitCode, _, stderr) = RunVconfig(
				$"decrypt --input \"{inputPath}\" --output \"{outputPath}\" --key-env TEST_KEY",
				new Dictionary<string, string> { ["TEST_KEY"] = aesKey });

			Assert.That(exitCode, Is.Not.EqualTo(0),
				"decrypt of a plaintext value must fail (no silent fallthrough)");
			Assert.That(stderr, Does.Contain("$.Database.ConnectionString"),
				"error must report the full JSON path, not just the leaf key");
		}

		[Test]
		public void Decrypt_KeyStartingWithDigit_ReportedInBracketNotation()
		{
			// "123" is not a valid JSONPath dot-notation identifier (must start with letter/_),
			// so it must use bracket notation `$['123']` rather than `$.123`.
			var aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

			var json = "{\n" +
				"  \"Items\": {\n" +
				"    \"123\": \"plaintext-not-ciphertext\"\n" +
				"  }\n" +
				"}\n";
			var inputPath = Path.Combine(_tempDir, "config.json");
			var outputPath = Path.Combine(_tempDir, "out.json");
			File.WriteAllText(inputPath, json);

			var (exitCode, _, stderr) = RunVconfig(
				$"decrypt --input \"{inputPath}\" --output \"{outputPath}\" --key-env TEST_KEY",
				new Dictionary<string, string> { ["TEST_KEY"] = aesKey });

			Assert.That(exitCode, Is.Not.EqualTo(0));
			Assert.That(stderr, Does.Contain("$.Items['123']"),
				"key starting with a digit must use bracket notation");
		}

		[Test]
		public void Decrypt_KeyWithDots_ReportedInBracketNotation()
		{
			// ASP.NET Core configs commonly use keys like "Microsoft.Hosting.Lifetime".
			// Dot-notation would render that as `$.Microsoft.Hosting.Lifetime` — looks like
			// three levels of nesting. Bracket notation disambiguates.
			var aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

			var json = "{\n" +
				"  \"Logging\": {\n" +
				"    \"LogLevel\": {\n" +
				"      \"Microsoft.Hosting.Lifetime\": \"plaintext-not-ciphertext\"\n" +
				"    }\n" +
				"  }\n" +
				"}\n";
			var inputPath = Path.Combine(_tempDir, "config.json");
			var outputPath = Path.Combine(_tempDir, "out.json");
			File.WriteAllText(inputPath, json);

			var (exitCode, _, stderr) = RunVconfig(
				$"decrypt --input \"{inputPath}\" --output \"{outputPath}\" --key-env TEST_KEY",
				new Dictionary<string, string> { ["TEST_KEY"] = aesKey });

			Assert.That(exitCode, Is.Not.EqualTo(0));
			Assert.That(stderr, Does.Contain("$.Logging.LogLevel['Microsoft.Hosting.Lifetime']"),
				"key with dots must be rendered in bracket notation");
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
