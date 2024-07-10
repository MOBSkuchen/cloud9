using System.Security.AccessControl;
using DokanNet;
using FileAccess = System.IO.FileAccess;

namespace cloud9lib;

public class CloneDriver : IClientBlueprint
{
    private InstanceData _instanceData;
    
    public CloneDriver(InstanceData instanceData)
    {
        _instanceData = instanceData;
    }
    
    public String ConvertFmt(String previous)
    {
        previous = Path.Join(_instanceData.RemotePath, previous);
        return previous.Replace("\\", "/");
    }

    public void Close() { }
    
    public static void RecursiveDelete(DirectoryInfo baseDir)
    {
        if (!baseDir.Exists)
            return;

        foreach (var dir in baseDir.EnumerateDirectories())
        {
            RecursiveDelete(dir);
        }
        baseDir.Delete(true);
    }

    public bool DeletePath(string path)
    {
        path = ConvertFmt(path);
        try
        {
            RecursiveDelete(new DirectoryInfo(path));
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
    public FileInformation ConstructFileInfo(String path)
    {
        return ConvertFileInfo(new FileInfo(path));
    }

    public FileInformation ConstructDirInfo(String path)
    {
        return ConvertDirInfo(new DirectoryInfo(path));
    }

    public FileInformation ConvertFileInfo(object item_)
    {
        FileInfo item = (FileInfo) item_;
        
        return new FileInformation {
            FileName = item.Name,
            Attributes = FileAttributes.Normal,
            LastAccessTime = DateTime.Now,
            LastWriteTime = item.LastWriteTime,
            CreationTime = item.CreationTime,
            Length = item.Length
        };
    }
    
    public FileInformation ConvertDirInfo(object item_)
    {
        DirectoryInfo item = (DirectoryInfo) item_;
        
        return new FileInformation {
            FileName = item.Name,
            Attributes = FileAttributes.Directory,
            LastAccessTime = DateTime.Now,
            LastWriteTime = item.LastWriteTime,
            CreationTime = item.CreationTime
        };
    }

    public List<FileInformation> ListFiles(string path)
    {
        path = ConvertFmt(path);
        List<FileInformation> files = new List<FileInformation>();
        foreach (var item in Directory.EnumerateFiles(path))
        {
            files.Add(ConstructFileInfo(item));
        }

        foreach (var item in Directory.EnumerateDirectories(path))
        {
            files.Add(ConstructDirInfo(item));
        }
        return files;
    }

    public bool FileExists(string path)
    {
        path = ConvertFmt(path);
        return Path.Exists(path);
    }

    public void SetFileTimes(string path, DateTime? atime, DateTime? mtime, DateTime? ctime)
    {
        path = ConvertFmt(path);
        if (atime != null) {File.SetLastAccessTime(path, atime.Value);}
        if (mtime != null) {File.SetLastWriteTime(path, mtime.Value);}
        if (ctime != null) {File.SetCreationTime(path, ctime.Value);}
    }

    public void CreateFile(string path)
    {
        path = ConvertFmt(path);
        File.Create(path);
    }

    public void CreateDirectory(string path)
    {
        path = ConvertFmt(path);
        Directory.CreateDirectory(path);
    }

    public void MoveFile(string oldpath, string newpath)
    {
        oldpath = ConvertFmt(oldpath);
        newpath = ConvertFmt(newpath);
        File.Move(oldpath, newpath);
    }

    public FileInformation GetFileInfo(string path)
    {
        path = ConvertFmt(path);
        return ConstructFileInfo(path);
    }

    public (long totalBytes, long freeBytes) GetDriveSize()
    {
        string rootDir = Directory.GetDirectoryRoot(_instanceData.RemotePath);
        DriveInfo driveInfo = new DriveInfo(rootDir);
        return (driveInfo.TotalSize, driveInfo.AvailableFreeSpace);
    }

    public void SetFileAttributes(string path, FileAttributes fileAttributes)
    {
        path = ConvertFmt(path);
        File.SetAttributes(path, fileAttributes);
    }

    public Stream GetFileStream(string path, FileMode mode, System.IO.FileAccess access, FileShare share)
    {
        path = ConvertFmt(path);
        return File.Open(path, mode, access, share);
    }

    public bool IsDirectory(string path)
    {
        path = ConvertFmt(path);
        return File.GetAttributes(path) == FileAttributes.Directory;
    }

    public FileSystemSecurity GetFileSystemSecurity(string path)
    {
        path = ConvertFmt(path);
        return (new FileInfo(path)).GetAccessControl();
    }

    public List<FileInformation> PatternSearch(string path)
    {
        throw new NotImplementedException();
    }

    public int IoReadAction(object fileStream, byte[] buffer, long offset)
    {
        FileStream fileStreamT = (FileStream) fileStream;
        fileStreamT.Seek(offset, SeekOrigin.Begin);
        return fileStreamT.Read(buffer, 0, buffer.Length);
    }

    public int IoWriteAction(object fileStream, byte[] buffer, long offset)
    {
        FileStream fileStreamT = (FileStream) fileStream;
        fileStreamT.Seek(offset, SeekOrigin.Begin);
        fileStreamT.Write(buffer);
        return buffer.Length;
    }

    public void CloseHandle(object fileStream)
    {
        FileStream fileStreamT = (FileStream) fileStream;
        fileStreamT.Close();
    }

    public void FlushHandle(object fileStream)
    {
        FileStream fileStreamT = (FileStream) fileStream;
        fileStreamT.Flush();
    }

    public void SetIoLength(object fileStream, long length)
    {
        FileStream fileStreamT = (FileStream) fileStream;
        fileStreamT.SetLength(length);
    }
}