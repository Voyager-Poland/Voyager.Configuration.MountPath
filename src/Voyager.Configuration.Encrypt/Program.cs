// Voyager Configuration DES Encrypt Tool
//
// Encrypts a single text value using the legacy DES algorithm.
// The DES key is read from an environment variable (default: ASPNETCORE_ENCODEKEY).
//
// Usage:
//   VoyagerEncrypt <text> [--key-env <ENV_VAR>]
//
// Options:
//   <text>              The plain-text value to encrypt.
//   --key-env, -e       Name of the environment variable holding the DES key
//                       (default: ASPNETCORE_ENCODEKEY).
//   --help, -h          Show this help message.
//
// Example:
//   set ASPNETCORE_ENCODEKEY=MySecret
//   VoyagerEncrypt "my-password"
//
// Note: This tool uses the legacy DES cipher for backward compatibility.
//       For new deployments, prefer Voyager.Configuration.Tool (AES-256-GCM).

using System.CommandLine;
using Voyager.Configuration.MountPath.Encryption;

var textArg = new Argument<string>("text", "The plain-text value to encrypt");

var keyEnvOption = new Option<string>(
		aliases: new[] { "--key-env", "-e" },
		getDefaultValue: () => "ASPNETCORE_ENCODEKEY",
		description: "Name of the environment variable holding the DES key");

var rootCommand = new RootCommand(
		"DES Encrypt Tool - Encrypts a single value using the legacy DES cipher.\n" +
		"Intended for generating values compatible with Voyager.Configuration.MountPath v1.\n" +
		"For new projects use Voyager.Configuration.Tool (AES-256-GCM).");

rootCommand.AddArgument(textArg);
rootCommand.AddOption(keyEnvOption);

rootCommand.SetHandler((string text, string keyEnv) =>
{
		var desKey = Environment.GetEnvironmentVariable(keyEnv);
		if (string.IsNullOrEmpty(desKey))
		{
				Console.Error.WriteLine($"Error: Environment variable '{keyEnv}' is not set or empty.");
				Environment.Exit(1);
		}

		var encryptor = new Encryptor(desKey!);
		Console.WriteLine($"Plain text : {text}");
		Console.WriteLine($"Encrypted  : {encryptor.Encrypt(text)}");
},
textArg,
keyEnvOption);

return await rootCommand.InvokeAsync(args);
