using System.Security.Cryptography;
using Voyager.Configuration.MountPath.Encryption;

namespace Voyager.Configuration.MountPath.Test
{
	[TestFixture]
	public class VersionedEncryptorTest
	{
		private const string LegacyDesKey = "LegacyDesKey1234567890";

		private static string GenerateBase64AesKey()
		{
			return Convert.ToBase64String(RandomNumberGenerator.GetBytes(AesGcmCipherProvider.KeySizeBytes));
		}

		private static VersionedEncryptor NewEncryptor(
			bool allowLegacyDes = true,
			Action<string>? warningLogger = null)
		{
			return new VersionedEncryptor(
				new AesGcmCipherProvider(GenerateBase64AesKey()),
				new Encryptor(LegacyDesKey),
				allowLegacyDes,
				warningLogger);
		}

		[Test]
		public void Encrypt_EmitsV2Prefix()
		{
			using var encryptor = NewEncryptor();

			var result = encryptor.Encrypt("payload");

			Assert.That(result, Does.StartWith(VersionedEncryptor.V2Prefix));
		}

		[Test]
		public void EncryptDecrypt_RoundTrip_ReturnsOriginal()
		{
			using var encryptor = NewEncryptor();
			var plaintext = "hello — secret";

			var ciphertext = encryptor.Encrypt(plaintext);
			var decrypted = encryptor.Decrypt(ciphertext);

			Assert.That(decrypted, Is.EqualTo(plaintext));
		}

		[Test]
		public void Decrypt_LegacyDesValue_ReturnsOriginal()
		{
			var legacyEncryptor = new Encryptor(LegacyDesKey);
			var legacyCiphertext = legacyEncryptor.Encrypt("legacy payload");
			using var encryptor = NewEncryptor();

			var decrypted = encryptor.Decrypt(legacyCiphertext);

			Assert.That(decrypted, Is.EqualTo("legacy payload"));
		}

		[Test]
		public void Decrypt_LegacyValue_InvokesWarningLoggerOnce()
		{
			var warnings = new List<string>();
			using var encryptor = NewEncryptor(warningLogger: warnings.Add);
			var legacy1 = new Encryptor(LegacyDesKey).Encrypt("a");
			var legacy2 = new Encryptor(LegacyDesKey).Encrypt("b");

			encryptor.Decrypt(legacy1);
			encryptor.Decrypt(legacy2);

			Assert.That(warnings, Has.Count.EqualTo(1));
			Assert.That(warnings[0], Does.Contain("vconfig reencrypt"));
		}

		[Test]
		public void Decrypt_LegacyValue_WhenAllowLegacyDesFalse_Throws()
		{
			using var encryptor = NewEncryptor(allowLegacyDes: false);
			var legacyCiphertext = new Encryptor(LegacyDesKey).Encrypt("payload");

			var ex = Assert.Throws<EncryptionException>(() => encryptor.Decrypt(legacyCiphertext));
			Assert.That(ex!.Message, Does.Contain("AllowLegacyDes"));
		}

		[Test]
		public void Decrypt_V2_TamperedPayload_ThrowsCryptographicException()
		{
			using var encryptor = NewEncryptor();
			var ciphertext = encryptor.Encrypt("payload");

			var bytes = Convert.FromBase64String(ciphertext.Substring(VersionedEncryptor.V2Prefix.Length));
			bytes[AesGcmCipherProvider.NonceSizeBytes] ^= 0x01;
			var tampered = VersionedEncryptor.V2Prefix + Convert.ToBase64String(bytes);

			Assert.Throws<AuthenticationTagMismatchException>(() => encryptor.Decrypt(tampered));
		}

		[Test]
		public void Decrypt_V2_WithWrongAesKey_ThrowsCryptographicException()
		{
			using var writer = NewEncryptor();
			using var reader = NewEncryptor();
			var ciphertext = writer.Encrypt("secret");

			Assert.Throws<AuthenticationTagMismatchException>(() => reader.Decrypt(ciphertext));
		}

		[Test]
		public void Decrypt_V2_MalformedBase64_ThrowsEncryptionException()
		{
			using var encryptor = NewEncryptor();

			var ex = Assert.Throws<EncryptionException>(
				() => encryptor.Decrypt(VersionedEncryptor.V2Prefix + "not valid base64 !!!"));
			Assert.That(ex!.Message, Does.Contain("Base64"));
		}

		[Test]
		public void Decrypt_V2_WithoutAesConfigured_ThrowsEncryptionException()
		{
			using var encryptor = new VersionedEncryptor(
				aes: null,
				legacyDes: new Encryptor(LegacyDesKey));

			var ex = Assert.Throws<EncryptionException>(
				() => encryptor.Decrypt(VersionedEncryptor.V2Prefix + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"));
			Assert.That(ex!.Message, Does.Contain("vconfig keygen"));
		}

		[Test]
		public void Encrypt_WithoutAesConfigured_ThrowsEncryptionException()
		{
			using var encryptor = new VersionedEncryptor(aes: null, legacyDes: new Encryptor(LegacyDesKey));

			Assert.Throws<EncryptionException>(() => encryptor.Encrypt("x"));
		}

		[Test]
		public void Decrypt_MixedValues_BothFormatsDecodeCorrectly()
		{
			var aesKey = GenerateBase64AesKey();
			var legacyEncryptor = new Encryptor(LegacyDesKey);
			using var encryptor = new VersionedEncryptor(
				new AesGcmCipherProvider(aesKey),
				legacyEncryptor);
			using var sameAesReader = new VersionedEncryptor(
				new AesGcmCipherProvider(aesKey),
				new Encryptor(LegacyDesKey));

			var v2Value = encryptor.Encrypt("new-style");
			var desValue = legacyEncryptor.Encrypt("old-style");

			Assert.That(sameAesReader.Decrypt(v2Value), Is.EqualTo("new-style"));
			Assert.That(sameAesReader.Decrypt(desValue), Is.EqualTo("old-style"));
		}

		[Test]
		public void Encrypt_Null_ThrowsArgumentNullException()
		{
			using var encryptor = NewEncryptor();

			Assert.Throws<ArgumentNullException>(() => encryptor.Encrypt(null!));
		}

		[Test]
		public void Decrypt_Null_ThrowsArgumentNullException()
		{
			using var encryptor = NewEncryptor();

			Assert.Throws<ArgumentNullException>(() => encryptor.Decrypt(null!));
		}

		[Test]
		public void Decrypt_ConcurrentLegacyReads_EmitsWarningExactlyOnce()
		{
			var warnings = new System.Collections.Concurrent.ConcurrentBag<string>();
			using var encryptor = NewEncryptor(warningLogger: warnings.Add);
			var legacyEncryptor = new Encryptor(LegacyDesKey);
			var legacyCiphertexts = Enumerable.Range(0, 64)
				.Select(i => legacyEncryptor.Encrypt("payload-" + i))
				.ToArray();

			Parallel.ForEach(legacyCiphertexts, ct => encryptor.Decrypt(ct));

			Assert.That(warnings, Has.Count.EqualTo(1));
		}
	}
}
