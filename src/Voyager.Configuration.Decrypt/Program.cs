// See https://aka.ms/new-console-template for more information
if (args.Length == 0)
{
	Console.Write("Param is requeried");
	return;
}
const string ENVName = "ASPNETCORE_ENCODEKEY";


var encodeKey = Environment.GetEnvironmentVariable(ENVName);
if (string.IsNullOrEmpty(encodeKey))
{
	Console.Write("Lack of enviromentVariable ENVName");
	return;
}

Voyager.Configuration.MountPath.Encryption.Encryptor encryptor = new Voyager.Configuration.MountPath.Encryption.Encryptor(encodeKey);

Console.WriteLine($"To encrypt: {args[0]}");
Console.WriteLine();
Console.WriteLine(encryptor.Decrypt(args[0]));
