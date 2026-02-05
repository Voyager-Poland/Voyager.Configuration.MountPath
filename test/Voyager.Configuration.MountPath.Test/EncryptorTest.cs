using Voyager.Configuration.MountPath.Encryption;

namespace Voyager.Configuration.MountPath.Test
{
  [TestFixture]
  public class EncryptorTest
  {
    private Encryptor encryptor;

    [SetUp]
    public void SetUp()
    {
      encryptor = new Encryption.Encryptor("PowaznyTestks123456722228");
    }

    [TearDown]
    public void TearDown()
    {
    }

    [Test]
    public void EncryptAndDecrypt_WithValidText_ReturnsOriginalText()
    {
      var plainText = "tekst to encode może jednak ma być dłuższy";
      var encrypted = encryptor.Encrypt(plainText);
      Console.WriteLine(encrypted);
      Assert.That(encrypted, Is.Not.EqualTo(plainText));
      var decrypted = encryptor.Decrypt(encrypted);
      Assert.That(decrypted, Is.EqualTo(plainText));
    }
  }
}
