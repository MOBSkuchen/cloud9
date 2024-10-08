﻿using System.Globalization;
using System.Security.AccessControl;
using DokanNet;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using FileAccess = System.IO.FileAccess;

namespace cloud9lib;

public class SftpDriver : IClientBlueprint {
    
    private readonly SftpClient _client;
    private readonly SshClient _sshClient;
    private readonly Dictionary<string, FileInformation> _finfoCache = new ();
    private readonly InstanceData _instanceData;

    public SftpDriver(InstanceData instanceData)
    {
        if (!instanceData.IsKeyAuth) _sshClient = new SshClient(instanceData.Host, instanceData.Port, instanceData.Username, instanceData.Password);
        else _sshClient = new SshClient(instanceData.Host, instanceData.Port, instanceData.Username, new PrivateKeyFile(instanceData.Password));
        
        if (!instanceData.IsKeyAuth) _client = new SftpClient(instanceData.Host, instanceData.Port, instanceData.Username, instanceData.Password);
        else _client = new SftpClient(instanceData.Host, instanceData.Port, instanceData.Username, new PrivateKeyFile(instanceData.Password));
        _instanceData = instanceData;
        Connect();
    }

    private void UpdateFinfoCache()
    {
        while (true)
        {
            foreach (var kvp in _finfoCache)
            {
                UpdateFinfoCacheFor(kvp.Key);
            }
        }
    }
    
    public void UpdateFinfoCacheFor(String k)
    {
        var nval = ConstructFileInfoMNull(k);
        if (nval == null)
        {
            _finfoCache.Remove(k);
            return;
        }
        _finfoCache[k] = nval.Value;
    }
    
    public void Connect() {
        _client.Connect();
        _sshClient.Connect();
        Task taskUpdateFinfoCache = Task.Run((Action) UpdateFinfoCache);
    }

    public String ConvertFmt(String previous)
    {
        previous = Path.Join(_instanceData.RemotePath, previous);
        return previous.Replace("\\", "/");
    }

    public void Close() {
        _client.Disconnect();
        _client.Dispose();
    }

    public bool DeletePath(String path)
    {
        path = ConvertFmt(path);
        if (!_client.Exists(path)) return false;
        _client.Delete(path);
        UpdateFinfoCacheFor(path);
        return true;
    }

    public FileInformation? ConstructFileInfoMNull(String path)
    {
        path = ConvertFmt(path);
        if (!FileExists(path)) return null; return ConstructFileInfo(path);
    }

    public FileInformation ConstructFileInfo(String path)
    {
        return ConvertFileInfo(_client.Get(path));
    }

    public FileInformation ConvertFileInfo(object item_)
    {
        ISftpFile item = (ISftpFile) item_;
        
        return new FileInformation {
            FileName = item.Name,
            Attributes = item.IsDirectory ? FileAttributes.Directory : FileAttributes.Normal,
            LastAccessTime = DateTime.Now,
            LastWriteTime = item.LastWriteTime,
            CreationTime = null,
            Length = item.Attributes.Size
        };
    }

    public List<FileInformation> ListFiles(String path)
    {
        path = ConvertFmt(path);
        List<FileInformation> files = new List<FileInformation>();
        foreach (var item in _client.ListDirectory(path))
        {
            if (item.Name == "." || item.Name == "..") continue;
            files.Add(GetFileInfo(item.FullName));
        }

        return files;
    }

    public bool FileExists(String path)
    {
        path = ConvertFmt(path);
        return _client.Exists(path);
    }
    public void SetFileTimes(string path, DateTime? atime, DateTime? mtime, DateTime? ctime)
    {
        path = ConvertFmt(path);
        if (atime != null) {_client.SetLastAccessTime(path, atime.Value);}
        if (mtime != null) {_client.SetLastWriteTime(path, mtime.Value);}
        UpdateFinfoCacheFor(path);
    }
    
    public void CreateFile(String path)
    {
        path = ConvertFmt(path);
        _client.Create(path);
    }

    public void CreateDirectory(String path)
    {
        path = ConvertFmt(path);
        _client.CreateDirectory(path);
        UpdateFinfoCacheFor(path);
    }

    public void MoveFile(String oldpath, String newpath)
    {
        oldpath = ConvertFmt(oldpath);
        newpath = ConvertFmt(newpath);
        _client.Get(oldpath).MoveTo(newpath);
        UpdateFinfoCacheFor(newpath);
        UpdateFinfoCacheFor(oldpath);
    }

    public FileInformation GetFileInfo(String path)
    {
        path = ConvertFmt(path);
        if (!_finfoCache.ContainsKey(path)) _finfoCache[path] = ConstructFileInfo(path);
        return _finfoCache[path];
    }

    public (long totalBytes, long freeBytes) GetDriveSize()
    {
        long totalBytes = 0;
        long freeBytes = 0;
        
        var cmd = _sshClient.RunCommand("df");
        if (cmd.ExitStatus != 0) return (totalBytes, freeBytes);
        var dfOutput = cmd.Result;

        try
        {

            var lines = dfOutput.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.StartsWith("Filesystem")) continue;

                var parts = line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 6) continue;

                if (long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out long total))
                {
                    totalBytes += total * 1024;
                }

                if (long.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out long free))
                {
                    freeBytes += free * 1024;
                }
            }
        }
        catch (Exception e)
        {
            totalBytes = 0;
            freeBytes = 0;
        }
        return (totalBytes, freeBytes);
    }

    public void SetFileAttributes(string path, FileAttributes fileAttributes)
    {
        path = ConvertFmt(path);
        File.SetAttributes(path, fileAttributes);
    }

    public Stream GetFileStream(string path, FileMode mode, FileAccess access, FileShare share)
    {
        path = ConvertFmt(path);
        return _client.Open(path, mode, access);
    }

    public bool IsDirectory(string path)
    {
        path = ConvertFmt(path);
        return _client.GetAttributes(path).IsDirectory;
    }
    
    public FileSystemSecurity? GetFileSystemSecurity(string path) { return null; }
    
    public List<FileInformation> PatternSearch(string path)
    {
        return new List<FileInformation>();
    }
    
    public int IoReadAction(object fileStream, byte[] buffer, long offset)
    {
        SftpFileStream fileStreamT = (SftpFileStream) fileStream;
        fileStreamT.Seek(offset, SeekOrigin.Begin);
        return fileStreamT.Read(buffer, 0, buffer.Length);
    }

    public int IoWriteAction(object fileStream, byte[] buffer, long offset)
    {
        SftpFileStream fileStreamT = (SftpFileStream) fileStream;
        fileStreamT.Seek(offset, SeekOrigin.Begin);
        fileStreamT.Write(buffer);
        return buffer.Length;
    }

    public void CloseHandle(object fileStream)
    {
        SftpFileStream fileStreamT = (SftpFileStream) fileStream;
        fileStreamT.Close();
    }

    public void FlushHandle(object fileStream)
    {
        SftpFileStream fileStreamT = (SftpFileStream) fileStream;
        fileStreamT.Flush();
    }

    public void SetIoLength(object fileStream, long length)
    {
        SftpFileStream fileStreamT = (SftpFileStream) fileStream;
        fileStreamT.SetLength(length);
    }
}