// See https://aka.ms/new-console-template for more information
if (args.Length == 0)
{
	Console.Write("Param is requeried");
	return;
}

var encodeKey = Environment.GetEnvironmentVariable("ENCODE_KEY");
if (string.IsNullOrEmpty(encodeKey))
{
	Console.Write("Lack of enviromentVariable ENCODE_KEY");
	return;
}

Voyager.Configuration.MountPath.Encryption.Encryptor encryptor = new Voyager.Configuration.MountPath.Encryption.Encryptor(encodeKey);

Console.WriteLine($"To encrypt: {args[0]}");
Console.WriteLine();
Console.WriteLine(encryptor.Encrypt(args[0]));
