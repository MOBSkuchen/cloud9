using DokanNet;

namespace cloud9client;

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

    public void Disconnect() { }
    
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

    public FileInformation? ConstructFileInfoMNull(String path) { if (!FileExists(path)) return null; return ConstructFileInfo(path);}
    
    public FileInformation ConstructFileInfo(String path) {return ConvertFileInfo(new FileInfo(path)); }

    public FileInformation ConvertFileInfo(object item_)
    {
        FileInfo item = (FileInfo) item_;
        
        return new FileInformation {
            FileName = item.Name,
            Attributes = (item.Directory != null) ? FileAttributes.Directory : FileAttributes.Normal,
            LastAccessTime = DateTime.Now,
            LastWriteTime = item.LastWriteTime,
            CreationTime = item.CreationTime,
            Length = item.Length
        };
    }

    public List<FileInformation> ListFiles(string path)
    {
        List<FileInformation> files = new List<FileInformation>();
        foreach (var item in Directory.EnumerateFileSystemEntries(path))
        {
            if (item == "." || item == "..") continue;
            files.Add(GetFileInfo(item));
        }
        return files;
    }

    public bool FileExists(string path)
    {
        return Path.Exists(path);
    }

    public int ReadBuffer(string path, byte[] bytes, int length, int offset)
    {
        throw new NotImplementedException();
    }

    public void WriteBuffer(string path, byte[] buffer, int offset)
    {
        throw new NotImplementedException();
    }

    public void SetFileTimes(string path, DateTime? atime, DateTime? mtime)
    {
        File.SetLastAccessTime();
    }

    public void CreateFile(string path)
    {
        File.Create(path);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public void MoveFile(string oldpath, string newpath)
    {
        File.Move(oldpath, newpath);
    }

    public void SetFileSize(string path, long size)
    {
        var f = File.Open(path, FileMode.Open);
        f.SetLength(size);
    }

    public FileInformation GetFileInfo(string path)
    {
        var f = File.Open(path, FileMode.Open);
        f.
    }

    public (long totalBytes, long freeBytes) GetDriveSize()
    {
        throw new NotImplementedException();
    }
}