using DiskInspectorApp.Constants;
using DiskInspectorApp.Models;

namespace DiskInspectorApp;

internal class DirectoryAuditorService
{
	public static void CheckDirectoryIntegrity(string directoryPath)
	{
		var hashFilePath = Path.Combine(directoryPath, FileNames.HashesAndSignatures);

		var currentFilesData = GetFilesData(directoryPath);
		if (!File.Exists(hashFilePath))
		{
			SaveHashFile(currentFilesData, hashFilePath);
			return;
		}

		var initialFilesData = LoadHashFile(hashFilePath);
		List<string> modifiedFiles = [];
		List<string> deletedFiles = initialFilesData.Select(x => x.RelativePath).ToList();
		List<RenamedFile> renamedFiles = [];
		List<string> addedFiles = [];

		foreach (var file in currentFilesData)
		{
			var initialFile = initialFilesData.FirstOrDefault(x => x.RelativePath == file.RelativePath);
			if (initialFile != null)
			{
				if (initialFile.Hash != file.Hash || !initialFile.Signature.SequenceEqual(file.Signature))
				{
					modifiedFiles.Add(file.RelativePath);
				}
				deletedFiles.Remove(file.RelativePath);
				continue;
			}

			var renamedFile = initialFilesData.FirstOrDefault(x => x.Hash == file.Hash && x.Signature.SequenceEqual(file.Signature));
			if (renamedFile != null)
			{
				renamedFiles.Add(new(renamedFile.RelativePath, file.RelativePath));
				deletedFiles.Remove(renamedFile.RelativePath);
			}
			else
			{
				addedFiles.Add(file.RelativePath);
			}
		}

		if (modifiedFiles.Count == 0 && deletedFiles.Count == 0 && renamedFiles.Count == 0 && addedFiles.Count == 0)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("Файлы данного каталога не изменились после последнего запуска программы.");
		}
		else
		{
			modifiedFiles.ForEach(x =>
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine($"Изменился файл '{x}'");
			});
			deletedFiles.ForEach(x =>
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"Удалился файл '{x}'");
			});
			renamedFiles.ForEach(x =>
			{
				Console.ForegroundColor = ConsoleColor.Magenta;
				Console.WriteLine($"Переименовался файл с '{x.OldName}' на '{x.NewName}'");
			});
			addedFiles.ForEach(x =>
			{
				Console.ForegroundColor = ConsoleColor.Blue;
				Console.WriteLine($"Добавился файл '{x}'");
			});
		}

		SaveHashFile(currentFilesData, hashFilePath);
		Console.ResetColor();
	}

	private static List<FileData> GetFilesData(string directoryPath)
	{
		List<FileData> files = [];

		foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
		{
			if (Path.GetFileName(filePath) == FileNames.HashesAndSignatures)
				continue;

			var fileBytes = File.ReadAllBytes(filePath);

			files.Add(new()
			{
				RelativePath = Path.GetRelativePath(directoryPath, filePath),
				Hash = GetXorHash(fileBytes),
				Signature = GetSignature(fileBytes)
			});
		}

		return files;
	}

	private static uint GetXorHash(byte[] fileBytes)
	{
		uint hash = 0;

		for (int i = 0; i < fileBytes.Length; i += 2)
		{
			uint segment = fileBytes[i];
			if (i + 1 < fileBytes.Length)
				segment = (segment << 8) | fileBytes[i + 1];
			else
				segment <<= 8;

			hash ^= segment;
		}

		return hash;
	}

	private static byte[] GetSignature(byte[] fileBytes, int minSignatureLength = 4)
	{
		if (fileBytes.Length == 0) return [];

		int signatureLength = Math.Min(minSignatureLength, fileBytes.Length);
		int start = fileBytes.Length / 2 - signatureLength / 2;
		if (start < 0) start = 0;

		byte[] signature = new byte[signatureLength];
		Array.Copy(fileBytes, start, signature, 0, signatureLength);

		return signature;
	}

	private static void SaveHashFile(IEnumerable<FileData> filesData, string hashFilePath)
	{
		// Снимаем атрибут Hidden, если установлен
		FileAttributes attributes;
		if (File.Exists(hashFilePath))
		{
			attributes = File.GetAttributes(hashFilePath);
			if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
			{
				attributes &= ~FileAttributes.Hidden;
				File.SetAttributes(hashFilePath, attributes);
			}
		}

		using var stream = new FileStream(hashFilePath, FileMode.Create, FileAccess.Write);
		using var writer = new StreamWriter(stream);
		writer.Write(filesData.Count());
		foreach (var fileData in filesData)
		{
			writer.WriteLine();
			writer.Write(fileData.RelativePath);
			writer.Write("|");
			writer.Write(fileData.Hash);
			writer.Write("|");
			writer.Write(Convert.ToBase64String(fileData.Signature));
		}

		attributes = File.GetAttributes(hashFilePath);
		File.SetAttributes(hashFilePath, attributes | FileAttributes.Hidden);
	}

	private static List<FileData> LoadHashFile(string hashFilePath, char fileInfoSeparator = '|')
	{
		var filesData = new List<FileData>();

		using (var stream = new FileStream(hashFilePath, FileMode.Open, FileAccess.Read))
		using (var reader = new StreamReader(stream))
		{
			int count = int.Parse(reader.ReadLine()!);
			for (int i = 0; i < count; i++)
			{
				var fileInfo = reader.ReadLine()!.Split(fileInfoSeparator);

				filesData.Add(new FileData
				{
					RelativePath = fileInfo[0],
					Hash = uint.Parse(fileInfo[1]),
					Signature = Convert.FromBase64String(fileInfo[2])
				});
			}
		}

		return filesData;
	}

	private record RenamedFile(string OldName, string NewName);
}
