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

    [Test]
    public void DecodeJson_WithNumericValues_DoesNotThrow()
    {
      var source = new EncryptedJsonConfigurationSource()
      {
        Key = "PowaznyTestks123456722228",
        Path = Path.Combine(Directory.GetCurrentDirectory(), "config", "encoded_with_numbers.json")
      };
      source.ResolveFileProvider();
      var providerWithNumbers = new EncryptedJsonConfigurationProvider(source);

      Assert.DoesNotThrow(() => providerWithNumbers.Load());
    }

    [Test]
    public void DecodeJson_WithNumericValues_ReturnsNumericValuesAsString()
    {
      var source = new EncryptedJsonConfigurationSource()
      {
        Key = "PowaznyTestks123456722228",
        Path = Path.Combine(Directory.GetCurrentDirectory(), "config", "encoded_with_numbers.json")
      };
      source.ResolveFileProvider();
      var providerWithNumbers = new EncryptedJsonConfigurationProvider(source);
      providerWithNumbers.Load();

      providerWithNumbers.TryGet("MyTemplate:IdTemplate", out var idValue);
      providerWithNumbers.TryGet("MyTemplate:Name", out var nameValue);
      providerWithNumbers.TryGet("MyTemplate:IsActive", out var isActiveValue);
      providerWithNumbers.TryGet("MyTemplate:Score", out var scoreValue);

      Assert.That(idValue, Is.EqualTo("77"));
      Assert.That(nameValue, Is.EqualTo("tekst to encode może jednak ma być dłuższy"));
      Assert.That(isActiveValue, Is.EqualTo("True"));
      Assert.That(scoreValue, Is.EqualTo("3.14"));
    }
  }
}
