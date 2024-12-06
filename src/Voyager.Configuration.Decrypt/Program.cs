// See https://aka.ms/new-console-template for more information
var encodeKey = string.Empty;
if (args.Length == 0)
{
	Console.Write("Param is requeried");
	return;
}
else if (args.Length == 1)
{
	const string ENVName = "ASPNETCORE_ENCODEKEY";
	encodeKey = Environment.GetEnvironmentVariable(ENVName);
}
else
{
	encodeKey = args[1];
}
if (string.IsNullOrEmpty(encodeKey))
{
	Console.Write("Lack of enviromentVariable ENVName");
	return;
}

Voyager.Configuration.MountPath.Encryption.Encryptor encryptor = new Voyager.Configuration.MountPath.Encryption.Encryptor(encodeKey);

Console.WriteLine($"To encrypt: {args[0]}");
Console.WriteLine();
Console.WriteLine(encryptor.Decrypt(args[0]));
