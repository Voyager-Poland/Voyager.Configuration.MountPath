using System.Security.Cryptography;
using Voyager.Configuration.MountPath.Encryption;

namespace Voyager.Configuration.MountPath.Test
{
	[TestFixture]
	public class AesGcmCipherProviderTest
	{
		private static string GenerateBase64Key()
		{
			return Convert.ToBase64String(RandomNumberGenerator.GetBytes(AesGcmCipherProvider.KeySizeBytes));
		}

		[Test]
		public void Ctor_NullKey_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new AesGcmCipherProvider(null!));
		}

		[Test]
		public void Ctor_InvalidBase64_ThrowsEncryptionException()
		{
			var ex = Assert.Throws<EncryptionException>(() => new AesGcmCipherProvider("not valid base64 !!!"));
			Assert.That(ex!.Message, Does.Contain("Base64"));
			Assert.That(ex.Message, Does.Contain("vconfig keygen"));
		}

		[Test]
		public void Ctor_WrongKeyLength_ThrowsEncryptionException()
		{
			var shortKey = Convert.ToBase64String(new byte[16]);
			var ex = Assert.Throws<EncryptionException>(() => new AesGcmCipherProvider(shortKey));
			Assert.That(ex!.Message, Does.Contain("32 bytes"));
			Assert.That(ex.Message, Does.Contain("vconfig keygen"));
		}

		[Test]
		public void EncryptDecrypt_RoundTrip_ReturnsOriginalPlaintext()
		{
			using var provider = new AesGcmCipherProvider(GenerateBase64Key());
			var plaintext = "sensitive password: äöüł-€-秘密";

			var ciphertext = provider.Encrypt(plaintext);
			var decrypted = provider.Decrypt(ciphertext);

			Assert.That(decrypted, Is.EqualTo(plaintext));
		}

		[Test]
		public void EncryptDecrypt_EmptyString_RoundTrips()
		{
			using var provider = new AesGcmCipherProvider(GenerateBase64Key());

			var ciphertext = provider.Encrypt(string.Empty);
			var decrypted = provider.Decrypt(ciphertext);

			Assert.That(decrypted, Is.EqualTo(string.Empty));
		}

		[Test]
		public void Encrypt_ProducesExpectedLayout_NoncePlusCiphertextPlusTag()
		{
			using var provider = new AesGcmCipherProvider(GenerateBase64Key());
			var plaintext = "hello";

			var ciphertext = provider.Encrypt(plaintext);

			var plaintextBytes = System.Text.Encoding.UTF8.GetByteCount(plaintext);
			var expectedLength = AesGcmCipherProvider.NonceSizeBytes + plaintextBytes + AesGcmCipherProvider.TagSizeBytes;
			Assert.That(ciphertext, Has.Length.EqualTo(expectedLength));
		}

		[Test]
		public void Decrypt_TamperedCiphertextBody_ThrowsCryptographicException()
		{
			using var provider = new AesGcmCipherProvider(GenerateBase64Key());
			var ciphertext = provider.Encrypt("original payload");

			ciphertext[AesGcmCipherProvider.NonceSizeBytes] ^= 0x01;

			Assert.Throws<AuthenticationTagMismatchException>(() => provider.Decrypt(ciphertext));
		}

		[Test]
		public void Decrypt_TamperedTag_ThrowsCryptographicException()
		{
			using var provider = new AesGcmCipherProvider(GenerateBase64Key());
			var ciphertext = provider.Encrypt("original payload");

			ciphertext[ciphertext.Length - 1] ^= 0x01;

			Assert.Throws<AuthenticationTagMismatchException>(() => provider.Decrypt(ciphertext));
		}

		[Test]
		public void Decrypt_TamperedNonce_ThrowsCryptographicException()
		{
			using var provider = new AesGcmCipherProvider(GenerateBase64Key());
			var ciphertext = provider.Encrypt("original payload");

			ciphertext[0] ^= 0x01;

			Assert.Throws<AuthenticationTagMismatchException>(() => provider.Decrypt(ciphertext));
		}

		[Test]
		public void Decrypt_WrongKey_ThrowsCryptographicException()
		{
			using var encryptor = new AesGcmCipherProvider(GenerateBase64Key());
			using var otherKey = new AesGcmCipherProvider(GenerateBase64Key());
			var ciphertext = encryptor.Encrypt("secret");

			Assert.Throws<AuthenticationTagMismatchException>(() => otherKey.Decrypt(ciphertext));
		}

		[Test]
		public void Decrypt_PayloadTooShort_ThrowsEncryptionException()
		{
			using var provider = new AesGcmCipherProvider(GenerateBase64Key());

			Assert.Throws<EncryptionException>(() => provider.Decrypt(new byte[16]));
		}

		[Test]
		public void Encrypt_ThousandCalls_ProducesUniqueNonces()
		{
			using var provider = new AesGcmCipherProvider(GenerateBase64Key());
			const string plaintext = "same plaintext every time";
			var nonces = new HashSet<string>();

			for (var i = 0; i < 1000; i++)
			{
				var ciphertext = provider.Encrypt(plaintext);
				var nonce = Convert.ToBase64String(ciphertext, 0, AesGcmCipherProvider.NonceSizeBytes);
				Assert.That(nonces.Add(nonce), Is.True, $"Nonce collision at iteration {i}");
			}
		}

		[Test]
		public void Encrypt_SamePlaintextTwice_ProducesDifferentCiphertext()
		{
			using var provider = new AesGcmCipherProvider(GenerateBase64Key());
			var plaintext = "identical input";

			var first = provider.Encrypt(plaintext);
			var second = provider.Encrypt(plaintext);

			Assert.That(first, Is.Not.EqualTo(second));
		}

		[Test]
		public void Encrypt_AfterDispose_ThrowsObjectDisposedException()
		{
			var provider = new AesGcmCipherProvider(GenerateBase64Key());
			provider.Dispose();

			Assert.Throws<ObjectDisposedException>(() => provider.Encrypt("x"));
		}
	}
}
