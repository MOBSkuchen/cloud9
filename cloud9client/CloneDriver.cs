﻿using DokanNet;

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

    public FileInformation? ConstructFileInfoMNull(String path)
    {
        path = ConvertFmt(path);
        if (!FileExists(path)) return null; return ConstructFileInfo(path);
    }

    public FileInformation ConstructFileInfo(String path)
    {
        path = ConvertFmt(path);
        return ConvertFileInfo(new FileInfo(path));
    }

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
        path = ConvertFmt(path);
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
        path = ConvertFmt(path);
        return Path.Exists(path);
    }

    public int ReadBuffer(string path, byte[] bytes, int length, int offset)
    {
        path = ConvertFmt(path);
        var readStream = File.OpenRead(path);
        return readStream.Read(bytes, offset, length);
    }

    public void WriteBuffer(string path, byte[] buffer, int offset)
    {
        path = ConvertFmt(path);
        var writeBuffer = File.OpenWrite(path);
        writeBuffer.Write(buffer, offset, buffer.Length);
        writeBuffer.Flush();
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

    public void SetFileSize(string path, long size)
    {
        path = ConvertFmt(path);
        var f = File.Open(path, FileMode.Open);
        f.SetLength(size);
    }

    public FileInformation GetFileInfo(string path)
    {
        path = ConvertFmt(path);
        return ConstructFileInfo(path);
    }

    public (long totalBytes, long freeBytes) GetDriveSize()
    {
        return (0, 0);
    }

    public void SetFileAttributes(string path, FileAttributes fileAttributes)
    {
        path = ConvertFmt(path);
        File.SetAttributes(path, fileAttributes);
    }
}