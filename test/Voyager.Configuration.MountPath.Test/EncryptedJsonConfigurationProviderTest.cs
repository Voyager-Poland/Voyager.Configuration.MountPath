using Voyager.Configuration.MountPath.Encryption;

namespace Voyager.Configuration.MountPath.Test
{
  [TestFixture]
  public class EncryptedJsonConfigurationProviderTest
  {
    private EncryptedJsonConfigurationProvider provider;

    [SetUp]
    public void SetUp()
    {
      var source = new EncryptedJsonConfigurationSource() { Key = "PowaznyTestks123456722228", Path = Path.Combine(Directory.GetCurrentDirectory(), "config", "encoded.json") };
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
