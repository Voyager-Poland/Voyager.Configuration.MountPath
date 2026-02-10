using Microsoft.Extensions.Configuration;
using System.IO;

namespace Voyager.Configuration.MountPath.Test
{
	/// <summary>
	/// Tests for error handling scenarios including missing files and corrupted JSON.
	/// </summary>
	[TestFixture]
	public class ErrorHandlingTest
	{
		private string _testConfigPath = null!;

		[SetUp]
		public void SetUp()
		{
			_testConfigPath = Path.Combine(Path.GetTempPath(), "error-test", Guid.NewGuid().ToString());
			Directory.CreateDirectory(_testConfigPath);
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
		public void MissingRequiredFile_ThrowsFileNotFoundException()
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(_testConfigPath)
				.AddJsonFile("nonexistent.json", optional: false);

			Assert.Throws<FileNotFoundException>(() => builder.Build());
		}

		[Test]
		public void MissingOptionalFile_DoesNotThrow()
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(_testConfigPath)
				.AddJsonFile("nonexistent.json", optional: true);

			Assert.DoesNotThrow(() => builder.Build());
		}

		[Test]
		public void CorruptedJson_MissingClosingBrace_ThrowsInvalidDataException()
		{
			var corruptedFile = Path.Combine(_testConfigPath, "corrupted.json");
			File.WriteAllText(corruptedFile, @"{
				""Key"": ""Value""
			");  // Missing closing brace

			var builder = new ConfigurationBuilder()
				.SetBasePath(_testConfigPath)
				.AddJsonFile("corrupted.json", optional: false);

			// .NET wraps JSON parse errors in InvalidDataException
			Assert.Throws<InvalidDataException>(() => builder.Build());
		}

		[Test]
		public void CorruptedJson_InvalidSyntax_ThrowsInvalidDataException()
		{
			var corruptedFile = Path.Combine(_testConfigPath, "invalid.json");
			File.WriteAllText(corruptedFile, @"{
				""Key"": ""Value"",
				""InvalidKey""
			}");

			var builder = new ConfigurationBuilder()
				.SetBasePath(_testConfigPath)
				.AddJsonFile("invalid.json", optional: false);

			// .NET wraps JSON parse errors in InvalidDataException
			Assert.Throws<InvalidDataException>(() => builder.Build());
		}

		[Test]
		public void CorruptedJson_TrailingComma_IsAllowedByDotNet()
		{
			// .NET JSON configuration provider allows trailing commas
			var trailingFile = Path.Combine(_testConfigPath, "trailing.json");
			File.WriteAllText(trailingFile, @"{
				""Key1"": ""Value1"",
				""Key2"": ""Value2"",
			}");

			var builder = new ConfigurationBuilder()
				.SetBasePath(_testConfigPath)
				.AddJsonFile("trailing.json", optional: false);

			IConfiguration config = null!;
			Assert.DoesNotThrow(() => config = builder.Build());
			Assert.That(config["Key1"], Is.EqualTo("Value1"));
			Assert.That(config["Key2"], Is.EqualTo("Value2"));
		}

		[Test]
		public void EmptyJsonFile_DoesNotThrow()
		{
			var emptyFile = Path.Combine(_testConfigPath, "empty.json");
			File.WriteAllText(emptyFile, "{}");

			var builder = new ConfigurationBuilder()
				.SetBasePath(_testConfigPath)
				.AddJsonFile("empty.json", optional: false);

			IConfiguration config = null!;
			Assert.DoesNotThrow(() => config = builder.Build());
			Assert.That(config, Is.Not.Null);
		}

		[Test]
		public void EmptyFile_ThrowsInvalidDataException()
		{
			var emptyFile = Path.Combine(_testConfigPath, "empty.json");
			File.WriteAllText(emptyFile, "");

			var builder = new ConfigurationBuilder()
				.SetBasePath(_testConfigPath)
				.AddJsonFile("empty.json", optional: false);

			// .NET wraps JSON parse errors in InvalidDataException
			Assert.Throws<InvalidDataException>(() => builder.Build());
		}

		[Test]
		public void WhitespaceOnlyFile_ThrowsInvalidDataException()
		{
			var whitespaceFile = Path.Combine(_testConfigPath, "whitespace.json");
			File.WriteAllText(whitespaceFile, "   \n\t  ");

			var builder = new ConfigurationBuilder()
				.SetBasePath(_testConfigPath)
				.AddJsonFile("whitespace.json", optional: false);

			// .NET wraps JSON parse errors in InvalidDataException
			Assert.Throws<InvalidDataException>(() => builder.Build());
		}

		[Test]
		public void InvalidJsonType_ArrayAtRoot_ThrowsInvalidDataException()
		{
			var arrayFile = Path.Combine(_testConfigPath, "array.json");
			File.WriteAllText(arrayFile, @"[""item1"", ""item2""]");

			var builder = new ConfigurationBuilder()
				.SetBasePath(_testConfigPath)
				.AddJsonFile("array.json", optional: false);

			// .NET wraps format errors in InvalidDataException
			Assert.Throws<InvalidDataException>(() => builder.Build());
		}

		[Test]
		public void InvalidJsonType_StringAtRoot_ThrowsInvalidDataException()
		{
			var stringFile = Path.Combine(_testConfigPath, "string.json");
			File.WriteAllText(stringFile, @"""just a string""");

			var builder = new ConfigurationBuilder()
				.SetBasePath(_testConfigPath)
				.AddJsonFile("string.json", optional: false);

			// .NET wraps format errors in InvalidDataException
			Assert.Throws<InvalidDataException>(() => builder.Build());
		}

		[Test]
		public void FileWithBOM_LoadsCorrectly()
		{
			var bomFile = Path.Combine(_testConfigPath, "bom.json");
			var utf8WithBom = new System.Text.UTF8Encoding(true);
			File.WriteAllText(bomFile, @"{""Key"": ""Value""}", utf8WithBom);

			var builder = new ConfigurationBuilder()
				.SetBasePath(_testConfigPath)
				.AddJsonFile("bom.json", optional: false);

			IConfiguration config = null!;
			Assert.DoesNotThrow(() => config = builder.Build());
			Assert.That(config["Key"], Is.EqualTo("Value"));
		}

		[Test]
		public void MixedLineEndings_LoadsCorrectly()
		{
			var mixedFile = Path.Combine(_testConfigPath, "mixed.json");
			File.WriteAllText(mixedFile, "{\r\n\"Key1\": \"Value1\",\n\"Key2\": \"Value2\"\r}");

			var builder = new ConfigurationBuilder()
				.SetBasePath(_testConfigPath)
				.AddJsonFile("mixed.json", optional: false);

			IConfiguration config = null!;
			Assert.DoesNotThrow(() => config = builder.Build());
			Assert.That(config["Key1"], Is.EqualTo("Value1"));
			Assert.That(config["Key2"], Is.EqualTo("Value2"));
		}

		[Test]
		public void ValidJsonWithComments_IsAllowedByDotNet()
		{
			// .NET JSON configuration provider supports comments (JsonCommentHandling.Skip)
			var commentedFile = Path.Combine(_testConfigPath, "commented.json");
			File.WriteAllText(commentedFile, @"{
				// This is a comment
				""Key"": ""Value""
			}");

			var builder = new ConfigurationBuilder()
				.SetBasePath(_testConfigPath)
				.AddJsonFile("commented.json", optional: false);

			IConfiguration config = null!;
			Assert.DoesNotThrow(() => config = builder.Build());
			Assert.That(config["Key"], Is.EqualTo("Value"));
		}

		[Test]
		public void FileWithSpecialCharacters_LoadsCorrectly()
		{
			var specialFile = Path.Combine(_testConfigPath, "special.json");
			File.WriteAllText(specialFile, @"{
				""PolishChars"": ""żółć jaźń"",
				""Emoji"": ""🚀 test"",
				""Unicode"": ""\u0041\u0042\u0043""
			}");

			var builder = new ConfigurationBuilder()
				.SetBasePath(_testConfigPath)
				.AddJsonFile("special.json", optional: false);

			IConfiguration config = null!;
			Assert.DoesNotThrow(() => config = builder.Build());
			Assert.That(config["PolishChars"], Is.EqualTo("żółć jaźń"));
			Assert.That(config["Emoji"], Is.EqualTo("🚀 test"));
			Assert.That(config["Unicode"], Is.EqualTo("ABC"));
		}
	}
}
