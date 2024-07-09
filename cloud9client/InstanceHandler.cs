using System.Security.AccessControl;
using DokanNet;
using FileAccess = DokanNet.FileAccess;

namespace cloud9client;

internal class InstanceHandler : IDokanOperations
{
    private readonly IClientBlueprint _clientHandler;
    private readonly InstanceData _instanceData;
    
    #region DokanOperations member

    public InstanceHandler(InstanceData instanceData, IClientBlueprint clientHandler)
    {
        _clientHandler = clientHandler;
        _instanceData = instanceData;
    }

    public void Cleanup(string filename, IDokanFileInfo info) { }

    public void CloseFile(string filename, IDokanFileInfo info)
    {
        if (info.Context != null && info.Context.GetType() == typeof(FileStream))
        {
            FileStream fileStream = (FileStream) info.Context;
            fileStream.Close();
        }
    }

    public NtStatus CreateFile(
        string filename,
        FileAccess access,
        FileShare share,
        FileMode mode,
        FileOptions options,
        FileAttributes attributes,
        IDokanFileInfo info)
    {
        if (mode == FileMode.CreateNew || mode == FileMode.Create)
        {
            if (info.IsDirectory)
                _clientHandler.CreateDirectory(filename);
            else 
                _clientHandler.CreateFile(filename);
        }
        if (!_clientHandler.FileExists(filename))
        {
            return DokanResult.FileNotFound;
        }

        if (_clientHandler.IsDirectory(filename))
        {
            info.IsDirectory = true;
            return DokanResult.Success;
        } else if (!_clientHandler.IsDirectory(filename) && info.IsDirectory)
        {
            return DokanResult.NotADirectory;
        }
        System.IO.FileAccess neoAccess;
        if (access == FileAccess.ReadData)
        {
            neoAccess = System.IO.FileAccess.Read;
        }
        else if (access == FileAccess.WriteData)
        {
            neoAccess = System.IO.FileAccess.Write;
        }
        else
        {
            neoAccess = System.IO.FileAccess.ReadWrite;
        } 
        info.Context = _clientHandler.GetFileStream(filename, mode, neoAccess, share);
        return DokanResult.Success;
    }

    public NtStatus DeleteDirectory(string filename, IDokanFileInfo info) {
        if (!_clientHandler.FileExists(filename)) {return DokanResult.FileNotFound;}
        _clientHandler.DeletePath(filename);
        return DokanResult.Success;
    }

    public NtStatus DeleteFile(string filename, IDokanFileInfo info) {
        if (!_clientHandler.FileExists(filename)) {return DokanResult.FileNotFound;}
        _clientHandler.DeletePath(filename);
        return DokanResult.Success;
    }

    public NtStatus FlushFileBuffers(
        string filename,
        IDokanFileInfo info)
    {
        if (info.Context.GetType() != typeof(Stream)) { return DokanResult.InvalidHandle; }
        Stream fileStream = (Stream) info.Context;
        fileStream.Flush();
        return DokanResult.Success;
    }

    public NtStatus FindFiles(
        string filename,
        out IList<FileInformation> files,
        IDokanFileInfo info)
    {
        files = new List<FileInformation>();
        if (!_clientHandler.FileExists(filename)) {return DokanResult.FileNotFound;}
        files = _clientHandler.ListFiles(filename);
        return DokanResult.Success;
    }

    public NtStatus GetFileInformation(
        string filename,
        out FileInformation fileinfo,
        IDokanFileInfo info)
    {
        fileinfo = new FileInformation();
        if (!_clientHandler.FileExists(filename)) {return DokanResult.FileNotFound;}
        fileinfo = _clientHandler.GetFileInfo(filename);
        return DokanResult.Success;
    }

    public NtStatus MoveFile(
        string filename,
        string newname,
        bool replace,
        IDokanFileInfo info)
    {
        _clientHandler.MoveFile(filename, newname);
        return DokanResult.Success;
    }

    public NtStatus ReadFile(
        string filename,
        byte[] buffer,
        out int readBytes,
        long offset,
        IDokanFileInfo info)
    {
        readBytes = 0;
        if (!_clientHandler.FileExists(filename))
        {
            return DokanResult.FileNotFound;
        }
        if ((info.Context == null) || info.Context.GetType() != typeof(FileStream))
        {
            return DokanResult.InvalidHandle;
        }
        FileStream fileStream = (FileStream) info.Context;
        fileStream.Seek(offset, SeekOrigin.Begin);
        readBytes = fileStream.Read(buffer, 0, buffer.Length);
        return DokanResult.Success;
    }

    public NtStatus SetEndOfFile(string filename, long length, IDokanFileInfo info)
    {
        if (!_clientHandler.FileExists(filename)) {return DokanResult.FileNotFound;}

        if (info.Context.GetType() != typeof(FileStream))
        {
            return DokanResult.InvalidHandle;
        }
        ((FileStream) info.Context).SetLength(length);
        return DokanResult.Success;
    }

    public NtStatus SetAllocationSize(string filename, long length, IDokanFileInfo info)
    {
        if (!_clientHandler.FileExists(filename)) {return DokanResult.FileNotFound;}
        if (info.Context.GetType() != typeof(FileStream)) { return DokanResult.InvalidHandle; }
        ((FileStream) info.Context).SetLength(length);
        return DokanResult.Success;
    }

    public NtStatus SetFileAttributes(
        string filename,
        FileAttributes attr,
        IDokanFileInfo info)
    {
        _clientHandler.SetFileAttributes(filename, attr);
        return DokanResult.Success;
    }

    public NtStatus SetFileTime(
        string filename,
        DateTime? ctime,
        DateTime? atime,
        DateTime? mtime,
        IDokanFileInfo info)
    {
        if (!_clientHandler.FileExists(filename)) {return DokanResult.FileNotFound;}
        _clientHandler.SetFileTimes(filename, atime, mtime, ctime);
        return DokanResult.Success;
    }

    public NtStatus UnlockFile(string filename, long offset, long length, IDokanFileInfo info)
    {
        return DokanResult.Success;
    }
    
    public NtStatus LockFile(
        string filename,
        long offset,
        long length,
        IDokanFileInfo info)
    {
        return DokanResult.Success;
    }

    public NtStatus Mounted(string mountPoint, IDokanFileInfo info)
    {
        return DokanResult.Success;
    }

    public NtStatus Unmounted(IDokanFileInfo info)
    {
        return DokanResult.Success;
    }

    public NtStatus GetDiskFreeSpace(
        out long freeBytesAvailable,
        out long totalBytes,
        out long totalFreeBytes,
        IDokanFileInfo info)
    {            
        var size = _clientHandler.GetDriveSize();
        
        freeBytesAvailable = size.freeBytes;
        totalBytes = size.totalBytes;
        totalFreeBytes = size.freeBytes;
        return DokanResult.Success;
    }

    public NtStatus WriteFile(
        string filename,
        byte[] buffer,
        out int writtenBytes,
        long offset,
        IDokanFileInfo info)
    {
        writtenBytes = 0;
        if (!_clientHandler.FileExists(filename))
        {
            return DokanResult.FileNotFound;
        }
        if ((info.Context == null) || info.Context.GetType() != typeof(FileStream))
        {
            return DokanResult.InvalidHandle;
        }
        FileStream fileStream = (FileStream)info.Context;
        fileStream.Seek(offset, SeekOrigin.Begin);
        fileStream.Write(buffer);
        writtenBytes = buffer.Length;
        return DokanResult.Success;
    }

    public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features,
        out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
    {
        volumeLabel = _instanceData.DriveName;
        features = FileSystemFeatures.None;
        fileSystemName = String.Empty;
        maximumComponentLength = 256;
        return DokanResult.Success;
    }

    public NtStatus GetFileSecurity(string filename, out FileSystemSecurity security, AccessControlSections sections,
        IDokanFileInfo info)
    {
        security = null;
        return DokanResult.Error;
    }

    public NtStatus SetFileSecurity(string filename, FileSystemSecurity security, AccessControlSections sections,
        IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus EnumerateNamedStreams(string filename, IntPtr enumContext, out string streamName,
        out long streamSize, IDokanFileInfo info)
    {
        streamName = string.Empty;
        streamSize = 0;
        return DokanResult.NotImplemented;
    }

    public NtStatus FindStreams(string filename, out IList<FileInformation> streams, IDokanFileInfo info)
    {
        streams = new FileInformation[0];
        return DokanResult.NotImplemented;
    }

    public NtStatus FindFilesWithPattern(string filename, string searchPattern, out IList<FileInformation> files,
        IDokanFileInfo info)
    {
        files = new FileInformation[0];
        return DokanResult.NotImplemented;
    }

    #endregion DokanOperations member
}
