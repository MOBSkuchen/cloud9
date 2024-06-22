using DokanNet;
namespace cloud9client;

public interface IClientBlueprint
{
    public String ConvertFmt(String previous);
    public void Disconnect();
    public bool DeletePath(String path);
    public FileInformation? ConstructFileInfoMNull(String path);
    public FileInformation ConstructFileInfo(String path);
    public FileInformation ConvertFileInfo(object item);
    public List<FileInformation> ListFiles(String path);
    public bool FileExists(String path);
    public int ReadBuffer(String path, byte[] bytes, int length, int offset);
    public void WriteBuffer(String path, byte[] buffer, int offset);
    public void SetFileTimes(string path, DateTime? atime, DateTime? mtime);
    public void CreateFile(String path);
    public void CreateDirectory(String path);
    public void MoveFile(String oldpath, String newpath);
    public void SetFileSize(String path, long size);
    public FileInformation GetFileInfoCached(String path);
    public (long totalBytes, long freeBytes) GetDriveSize();
}