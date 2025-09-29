namespace DiskInspectorApp.Models;

internal class FileData
{
	public string RelativePath { get; set; } = "";

	public uint Hash { get; set; }
	
	public byte[] Signature { get; set; } = [];
}
