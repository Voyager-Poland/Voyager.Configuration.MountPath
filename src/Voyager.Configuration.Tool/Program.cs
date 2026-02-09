using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;
using Voyager.Configuration.MountPath.Encryption;

// Root command
var rootCommand = new RootCommand("Voyager Configuration Tool - Migration helper for JSON configuration encryption");

// Shared options
var keyOption = new Option<string?>(
    aliases: new[] { "--key", "-k" },
    description: "Encryption key (not recommended - use --key-env instead)");

var keyEnvOption = new Option<string>(
    aliases: new[] { "--key-env" },
    getDefaultValue: () => "ASPNETCORE_ENCODEKEY",
    description: "Environment variable containing encryption key");

// encrypt-value command (backward compatibility)
var encryptValueCommand = new Command("encrypt-value", "Encrypt single text value");
var encryptTextArg = new Argument<string>("text", "Text to encrypt");
encryptValueCommand.AddArgument(encryptTextArg);
encryptValueCommand.AddOption(keyOption);
encryptValueCommand.AddOption(keyEnvOption);
encryptValueCommand.SetHandler(async (string text, string? key, string keyEnv) =>
{
    try
    {
        var encryptionKey = GetEncryptionKey(key, keyEnv);
        var encryptor = new Encryptor(encryptionKey);
        var encrypted = encryptor.Encrypt(text);
        Console.WriteLine(encrypted);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
},
encryptTextArg,
keyOption,
keyEnvOption);

// decrypt-value command (backward compatibility)
var decryptValueCommand = new Command("decrypt-value", "Decrypt single text value");
var decryptTextArg = new Argument<string>("encrypted", "Encrypted text to decrypt");
decryptValueCommand.AddArgument(decryptTextArg);
decryptValueCommand.AddOption(keyOption);
decryptValueCommand.AddOption(keyEnvOption);
decryptValueCommand.SetHandler(async (string encrypted, string? key, string keyEnv) =>
{
    try
    {
        var encryptionKey = GetEncryptionKey(key, keyEnv);
        var encryptor = new Encryptor(encryptionKey);
        var decrypted = encryptor.Decrypt(encrypted);
        Console.WriteLine(decrypted);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
},
decryptTextArg,
keyOption,
keyEnvOption);

// encrypt command (JSON file)
var encryptCommand = new Command("encrypt", "Encrypt values in JSON configuration file");
var encryptInputOption = new Option<FileInfo>(
    aliases: new[] { "--input", "-i" },
    description: "Input plain JSON file") { IsRequired = true };
var encryptOutputOption = new Option<FileInfo?>(
    aliases: new[] { "--output", "-o" },
    description: "Output encrypted JSON file (default: overwrite input)");
var forceOption = new Option<bool>(
    aliases: new[] { "--force", "-f" },
    description: "Overwrite existing output file");
var inPlaceOption = new Option<bool>(
    "--in-place",
    description: "Encrypt file in-place (modify original)");

encryptCommand.AddOption(encryptInputOption);
encryptCommand.AddOption(encryptOutputOption);
encryptCommand.AddOption(keyOption);
encryptCommand.AddOption(keyEnvOption);
encryptCommand.AddOption(forceOption);
encryptCommand.AddOption(inPlaceOption);

encryptCommand.SetHandler(async (FileInfo input, FileInfo? output, string? key, string keyEnv, bool force, bool inPlace) =>
{
    try
    {
        ShowDeprecationWarning();

        if (!input.Exists)
        {
            Console.Error.WriteLine($"Error: Input file not found: {input.FullName}");
            Environment.Exit(1);
        }

        var encryptionKey = GetEncryptionKey(key, keyEnv);
        var encryptor = new Encryptor(encryptionKey);

        // Determine output file
        var outputFile = inPlace ? input : (output ?? input);

        if (outputFile.Exists && !force && !inPlace)
        {
            Console.Error.WriteLine($"Error: Output file already exists: {outputFile.FullName}");
            Console.Error.WriteLine("Use --force to overwrite or --in-place to modify original.");
            Environment.Exit(1);
        }

        Console.WriteLine($"Encrypting {input.FullName}...");

        // Read and parse JSON
        var jsonText = await File.ReadAllTextAsync(input.FullName);
        var jsonNode = JsonNode.Parse(jsonText);

        if (jsonNode == null)
        {
            Console.Error.WriteLine("Error: Invalid JSON file");
            Environment.Exit(1);
        }

        // Encrypt values
        var encryptedNode = EncryptJsonNode(jsonNode, encryptor);

        // Write output
        var options = new JsonSerializerOptions { WriteIndented = true };
        var encryptedJson = encryptedNode.ToJsonString(options);
        await File.WriteAllTextAsync(outputFile.FullName, encryptedJson);

        Console.WriteLine($"✓ Encrypted successfully: {outputFile.FullName}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
},
encryptInputOption,
encryptOutputOption,
keyOption,
keyEnvOption,
forceOption,
inPlaceOption);

// decrypt command (JSON file)
var decryptCommand = new Command("decrypt", "Decrypt values in JSON configuration file");
var decryptInputOption = new Option<FileInfo>(
    aliases: new[] { "--input", "-i" },
    description: "Input encrypted JSON file") { IsRequired = true };
var decryptOutputOption = new Option<FileInfo>(
    aliases: new[] { "--output", "-o" },
    description: "Output decrypted JSON file") { IsRequired = true };

decryptCommand.AddOption(decryptInputOption);
decryptCommand.AddOption(decryptOutputOption);
decryptCommand.AddOption(keyOption);
decryptCommand.AddOption(keyEnvOption);
decryptCommand.AddOption(forceOption);

decryptCommand.SetHandler(async (FileInfo input, FileInfo output, string? key, string keyEnv, bool force) =>
{
    try
    {
        if (!input.Exists)
        {
            Console.Error.WriteLine($"Error: Input file not found: {input.FullName}");
            Environment.Exit(1);
        }

        var encryptionKey = GetEncryptionKey(key, keyEnv);
        var encryptor = new Encryptor(encryptionKey);

        if (output.Exists && !force)
        {
            Console.Error.WriteLine($"Error: Output file already exists: {output.FullName}");
            Console.Error.WriteLine("Use --force to overwrite.");
            Environment.Exit(1);
        }

        Console.WriteLine($"Decrypting {input.FullName}...");

        // Read and parse JSON
        var jsonText = await File.ReadAllTextAsync(input.FullName);
        var jsonNode = JsonNode.Parse(jsonText);

        if (jsonNode == null)
        {
            Console.Error.WriteLine("Error: Invalid JSON file");
            Environment.Exit(1);
        }

        // Decrypt values
        var decryptedNode = DecryptJsonNode(jsonNode, encryptor);

        // Write output
        var options = new JsonSerializerOptions { WriteIndented = true };
        var decryptedJson = decryptedNode.ToJsonString(options);
        await File.WriteAllTextAsync(output.FullName, decryptedJson);

        Console.WriteLine($"✓ Decrypted successfully: {output.FullName}");
        Console.WriteLine();
        Console.WriteLine("You can now encrypt this file with SOPS:");
        Console.WriteLine($"  sops -e {output.FullName} > {input.FullName}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
},
decryptInputOption,
decryptOutputOption,
keyOption,
keyEnvOption,
forceOption);

// Add commands to root
rootCommand.AddCommand(encryptCommand);
rootCommand.AddCommand(decryptCommand);
rootCommand.AddCommand(encryptValueCommand);
rootCommand.AddCommand(decryptValueCommand);

// Execute
return await rootCommand.InvokeAsync(args);

// Helper methods
static string GetEncryptionKey(string? keyParam, string keyEnvVar)
{
    if (!string.IsNullOrWhiteSpace(keyParam))
    {
        Console.WriteLine("⚠️  Warning: Passing key via --key is not secure. Use --key-env instead.");
        return keyParam;
    }

    var key = Environment.GetEnvironmentVariable(keyEnvVar);
    if (string.IsNullOrWhiteSpace(key))
    {
        throw new Exception($"Encryption key not found. Set environment variable '{keyEnvVar}' or use --key-env option.");
    }

    return key;
}

static void ShowDeprecationWarning()
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine();
    Console.WriteLine("⚠️  WARNING: Built-in encryption is DEPRECATED");
    Console.WriteLine("This tool is provided for migration purposes only.");
    Console.WriteLine("For new projects, use Mozilla SOPS instead.");
    Console.WriteLine("See: https://github.com/mozilla/sops");
    Console.WriteLine();
    Console.ResetColor();
}

static JsonNode EncryptJsonNode(JsonNode node, IEncryptor encryptor)
{
    if (node is JsonValue value)
    {
        // Encrypt only string values
        if (value.TryGetValue<string>(out var str))
        {
            return JsonValue.Create(encryptor.Encrypt(str));
        }
        // Numbers, booleans remain unchanged
        return value.DeepClone();
    }
    else if (node is JsonObject obj)
    {
        var result = new JsonObject();
        foreach (var (key, val) in obj)
        {
            result[key] = val != null ? EncryptJsonNode(val, encryptor) : null;
        }
        return result;
    }
    else if (node is JsonArray arr)
    {
        var result = new JsonArray();
        foreach (var item in arr)
        {
            result.Add(item != null ? EncryptJsonNode(item, encryptor) : null);
        }
        return result;
    }
    return node.DeepClone();
}

static JsonNode DecryptJsonNode(JsonNode node, IEncryptor encryptor)
{
    if (node is JsonValue value)
    {
        // Decrypt only string values
        if (value.TryGetValue<string>(out var str))
        {
            try
            {
                // Try to decrypt - if it fails, it might not be encrypted
                return JsonValue.Create(encryptor.Decrypt(str));
            }
            catch
            {
                // Not encrypted, return as-is
                return value.DeepClone();
            }
        }
        // Numbers, booleans remain unchanged
        return value.DeepClone();
    }
    else if (node is JsonObject obj)
    {
        var result = new JsonObject();
        foreach (var (key, val) in obj)
        {
            result[key] = val != null ? DecryptJsonNode(val, encryptor) : null;
        }
        return result;
    }
    else if (node is JsonArray arr)
    {
        var result = new JsonArray();
        foreach (var item in arr)
        {
            result.Add(item != null ? DecryptJsonNode(item, encryptor) : null);
        }
        return result;
    }
    return node.DeepClone();
}
