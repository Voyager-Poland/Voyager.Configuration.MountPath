using Voyager.Configuration.MountPath.Encryption;

namespace Voyager.Configuration.MountPath.Test
{
  class EncryptorTest
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
    public void DecoreEncode()
    {
      var tekxt = "tekst to encode może jednak ma być dłuższy";
      var result = encryptor.Encrypt(tekxt);
      Console.WriteLine(result);
      Assert.That(result, Is.Not.EqualTo(tekxt));
      var afterall = encryptor.Decrypt(result);
      Assert.That(afterall, Is.EqualTo(tekxt));
    }
  }

  class EncryptedJsonConfigurationProviderTest
  {
    private EncryptedJsonConfigurationProvider provider;

    [SetUp]
    public void SetUp()
    {
      var source = new EncryptedJsonConfigurationSource() { Key = "PowaznyTestks123456722228", Path = Path.Combine(Directory.GetCurrentDirectory(), "config\\encoded.json") };
      source.ResolveFileProvider();
      provider = new EncryptedJsonConfigurationProvider(source);

    }

    [Test]
    public void DecodeJson()
    {
      provider.Load();
      string readvalue = string.Empty;

      provider.TryGet("values", out readvalue!);

      Assert.That(readvalue, Is.EqualTo("tekst to encode może jednak ma być dłuższy"));

    }
  }
}
