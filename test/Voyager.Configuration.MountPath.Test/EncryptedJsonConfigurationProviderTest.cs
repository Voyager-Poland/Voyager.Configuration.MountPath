using Voyager.Configuration.MountPath.Encryption;

namespace Voyager.Configuration.MountPath.Test
{
  [TestFixture]
  public class EncryptedJsonConfigurationProviderTest
  {
    private EncryptedJsonConfigurationProvider _provider;

    [SetUp]
    public void SetUp()
    {
      var source = new EncryptedJsonConfigurationSource() { Key = "PowaznyTestks123456722228", Path = Path.Combine(Directory.GetCurrentDirectory(), "config", "encoded.json") };
      source.ResolveFileProvider();
      _provider = new EncryptedJsonConfigurationProvider(source);

    }

    [Test]
    public void DecodeJson()
    {
      _provider.Load();
      string readvalue = string.Empty;

      _provider.TryGet("values", out readvalue!);

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

    [Test]
    public void DecodeJson_MixedTypesWithEncryptedString_PreservesValues()
    {
      const string key = "PowaznyTestks123456722228";
      var encryptor = new Encryptor(key);
      var encryptedHaslo = encryptor.Encrypt("Ukrtyte");

      var encryptedJson = $$"""
        {
          "MyTemplate": {
            "IdTemplate": 77,
            "haslo": "{{encryptedHaslo}}",
            "logowanie": false
          }
        }
        """;

      var tempPath = Path.Combine(Path.GetTempPath(), $"encrypt_roundtrip_{Guid.NewGuid():N}.json");
      File.WriteAllText(tempPath, encryptedJson);

      try
      {
        var source = new EncryptedJsonConfigurationSource { Key = key, Path = tempPath };
        source.ResolveFileProvider();
        var mixedProvider = new EncryptedJsonConfigurationProvider(source);
        mixedProvider.Load();

        mixedProvider.TryGet("MyTemplate:IdTemplate", out var idValue);
        mixedProvider.TryGet("MyTemplate:haslo", out var hasloValue);
        mixedProvider.TryGet("MyTemplate:logowanie", out var logowanieValue);

        Assert.Multiple(() =>
        {
          Assert.That(idValue, Is.EqualTo("77"));
          Assert.That(hasloValue, Is.EqualTo("Ukrtyte"));
          Assert.That(logowanieValue, Is.EqualTo("False"));
        });
      }
      finally
      {
        if (File.Exists(tempPath))
          File.Delete(tempPath);
      }
    }
  }
}
