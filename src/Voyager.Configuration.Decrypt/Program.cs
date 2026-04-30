// Voyager Configuration DES Decrypt Tool
//
// Decrypts a single text value that was encrypted using the legacy DES algorithm.
// The DES key is read from an environment variable (default: ASPNETCORE_ENCODEKEY).
//
// Usage:
//   VoyagerDecrypt <encrypted> [--key-env <ENV_VAR>]
//
// Options:
//   <encrypted>         The DES-encrypted value to decrypt.
//   --key-env, -e       Name of the environment variable holding the DES key
//                       (default: ASPNETCORE_ENCODEKEY).
//   --help, -h          Show this help message.
//
// Example:
//   set ASPNETCORE_ENCODEKEY=MySecret
//   VoyagerDecrypt "abc123encryptedvalue=="
//
// Note: This tool uses the legacy DES cipher for backward compatibility.
//       For new deployments, prefer Voyager.Configuration.Tool (AES-256-GCM).

using System.CommandLine;
using Voyager.Configuration.MountPath.Encryption;

var encryptedArg = new Argument<string>("encrypted", "The DES-encrypted value to decrypt");

var keyEnvOption = new Option<string>(
		aliases: new[] { "--key-env", "-e" },
		getDefaultValue: () => "ASPNETCORE_ENCODEKEY",
		description: "Name of the environment variable holding the DES key");

var rootCommand = new RootCommand(
		"DES Decrypt Tool - Decrypts a single value encrypted with the legacy DES cipher.\n" +
		"Intended for reading values encrypted by Voyager.Configuration.MountPath v1.\n" +
		"For new projects use Voyager.Configuration.Tool (AES-256-GCM).");

rootCommand.AddArgument(encryptedArg);
rootCommand.AddOption(keyEnvOption);

rootCommand.SetHandler((string encrypted, string keyEnv) =>
{
		var desKey = Environment.GetEnvironmentVariable(keyEnv);
		if (string.IsNullOrEmpty(desKey))
		{
				Console.Error.WriteLine($"Error: Environment variable '{keyEnv}' is not set or empty.");
				Environment.Exit(1);
		}

		var encryptor = new Encryptor(desKey!);
		Console.WriteLine($"Encrypted  : {encrypted}");
		Console.WriteLine($"Decrypted  : {encryptor.Decrypt(encrypted)}");
},
encryptedArg,
keyEnvOption);

return await rootCommand.InvokeAsync(args);
