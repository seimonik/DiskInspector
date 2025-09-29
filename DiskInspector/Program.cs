// See https://aka.ms/new-console-template for more information
using DiskInspectorApp;

Console.Write("Введите полный путь к каталогу для проверки целостности данных: ");
var directoryPath = Console.ReadLine();

if (ValidateDirectoryPath(directoryPath!))
{
	DirectoryAuditorService.CheckDirectoryIntegrity(directoryPath!);
}
else
{
	Console.WriteLine("Введите корректный путь к каталогу.");
}

static bool ValidateDirectoryPath(string directoryPath)
{
	if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
		return true;
	return false;
}