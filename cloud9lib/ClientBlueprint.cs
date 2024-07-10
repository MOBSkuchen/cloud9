using System.Security.AccessControl;
using DokanNet;
namespace cloud9lib;

public interface IClientBlueprint
{
    // IMPORTANT!
    // The conversion of virtual path to real path
    // should from now on be handled by the client driver
    public String ConvertFmt(String previous);
    public void Close();
    public bool DeletePath(String path);
    public FileInformation? ConstructFileInfoMNull(String path);
    public FileInformation ConstructFileInfo(String path);
    public FileInformation ConvertFileInfo(object item);
    public List<FileInformation> ListFiles(String path);
    public bool FileExists(String path);
    public int ReadBuffer(String path, byte[] bytes, int length, int offset);
    public void WriteBuffer(String path, byte[] buffer, int offset);
    // atime : Last access time
    // mtime : Last write time
    // ctime : Creation time
    public void SetFileTimes(string path, DateTime? atime, DateTime? mtime, DateTime? ctime);
    public void CreateFile(String path);
    public void CreateDirectory(String path);
    public void MoveFile(String oldpath, String newpath);
    public void SetFileSize(String path, long size);
    public FileInformation GetFileInfo(String path);
    public (long totalBytes, long freeBytes) GetDriveSize();
    public void SetFileAttributes(String path, FileAttributes fileAttributes);
    public Stream GetFileStream(string path, FileMode mode, System.IO.FileAccess access, FileShare share);
    public bool IsDirectory(string path);
    // Return null if this is not supported
    public FileSystemSecurity? GetFileSystemSecurity(string path);
    public List<FileInformation> PatternSearch(String path);
    public int IoReadAction(object fileStream, byte[] buffer, long offset);
    public int IoWriteAction(object fileStream, byte[] buffer, long offset);
    public void CloseHandle(object fileStream);
    public void FlushHandle(object fileStream);
    public void SetIoLength(object fileStream, long length);
}