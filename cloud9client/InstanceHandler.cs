﻿using System.Security.AccessControl;
using DokanNet;
using FileAccess = DokanNet.FileAccess;

namespace cloud9client;

internal class InstanceHandler : IDokanOperations
{
    private readonly IClientBlueprint _clientHandler;
    public readonly String Label;
    public readonly String Name;
    
    #region DokanOperations member

    public InstanceHandler(String label, String name, IClientBlueprint clientHandler)
    {
        _clientHandler = clientHandler;
        Label = label;
        Name = name;
    }

    public void Cleanup(string filename, IDokanFileInfo info)
    {
    }

    public void CloseFile(string filename, IDokanFileInfo info)
    {
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
        if (mode != FileMode.CreateNew) return DokanResult.Success;
        filename = _clientHandler.ConvertFmt(filename);
        if (info.IsDirectory)
            _clientHandler.CreateDirectory(filename);
        else 
            _clientHandler.CreateFile(filename);
        return DokanResult.Success;
    }

    public NtStatus DeleteDirectory(string filename, IDokanFileInfo info) {
        if (!_clientHandler.FileExists(filename)) {return DokanResult.FileNotFound;}
        _clientHandler.DeletePath(_clientHandler.ConvertFmt(filename));
        return DokanResult.Success;
    }

    public NtStatus DeleteFile(string filename, IDokanFileInfo info) {
        if (!_clientHandler.FileExists(filename)) {return DokanResult.FileNotFound;}
        _clientHandler.DeletePath(_clientHandler.ConvertFmt(filename));
        return DokanResult.Success;
    }

    public NtStatus FlushFileBuffers(
        string filename,
        IDokanFileInfo info)
    {
        return DokanResult.Success;
    }

    public NtStatus FindFiles(
        string filename,
        out IList<FileInformation> files,
        IDokanFileInfo info)
    {
        filename = _clientHandler.ConvertFmt(filename);
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
        filename = _clientHandler.ConvertFmt(filename);
        fileinfo = new FileInformation();
        if (!_clientHandler.FileExists(filename)) {return DokanResult.FileNotFound;}
        fileinfo = _clientHandler.GetFileInfoCached(filename);
        return DokanResult.Success;
    }

    public NtStatus LockFile(
        string filename,
        long offset,
        long length,
        IDokanFileInfo info)
    {
        filename = _clientHandler.ConvertFmt(filename);
        return DokanResult.Success;
    }

    public NtStatus MoveFile(
        string filename,
        string newname,
        bool replace,
        IDokanFileInfo info)
    {
        filename = _clientHandler.ConvertFmt(filename);
        newname = _clientHandler.ConvertFmt(newname);
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
        filename = _clientHandler.ConvertFmt(filename);
        readBytes = 0;
        if (!_clientHandler.FileExists(filename)) {return DokanResult.FileNotFound;}
        readBytes = _clientHandler.ReadBuffer(filename, buffer, buffer.Length, Convert.ToInt32(offset));
        return DokanResult.Success;
    }

    public NtStatus SetEndOfFile(string filename, long length, IDokanFileInfo info)
    {
        filename = _clientHandler.ConvertFmt(filename);
        _clientHandler.SetFileSize(filename, length);
        return DokanResult.Success;
    }

    public NtStatus SetAllocationSize(string filename, long length, IDokanFileInfo info)
    {
        filename = _clientHandler.ConvertFmt(filename);
        _clientHandler.SetFileSize(filename, length);
        return DokanResult.Success;
    }

    public NtStatus SetFileAttributes(
        string filename,
        FileAttributes attr,
        IDokanFileInfo info)
    {
        filename = _clientHandler.ConvertFmt(filename);
        return DokanResult.Success;
    }

    public NtStatus SetFileTime(
        string filename,
        DateTime? ctime,
        DateTime? atime,
        DateTime? mtime,
        IDokanFileInfo info)
    {
        filename = _clientHandler.ConvertFmt(filename);
        if (!_clientHandler.FileExists(filename)) {return DokanResult.FileNotFound;}
        _clientHandler.SetFileTimes(filename, atime, mtime);
        return DokanResult.Success;
    }

    public NtStatus UnlockFile(string filename, long offset, long length, IDokanFileInfo info)
    {
        filename = _clientHandler.ConvertFmt(filename);
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
        filename = _clientHandler.ConvertFmt(filename);
        Task.Run(() => _clientHandler.WriteBuffer(filename, buffer, Convert.ToInt32(offset)));
        writtenBytes = buffer.Length;
        return DokanResult.Success;
    }

    public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features,
        out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
    {
        volumeLabel = Label;
        features = FileSystemFeatures.None;
        fileSystemName = Name;
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
        filename = _clientHandler.ConvertFmt(filename);
        streamName = string.Empty;
        streamSize = 0;
        return DokanResult.NotImplemented;
    }

    public NtStatus FindStreams(string filename, out IList<FileInformation> streams, IDokanFileInfo info)
    {
        filename = _clientHandler.ConvertFmt(filename);
        streams = new FileInformation[0];
        return DokanResult.NotImplemented;
    }

    public NtStatus FindFilesWithPattern(string filename, string searchPattern, out IList<FileInformation> files,
        IDokanFileInfo info)
    {
        filename = _clientHandler.ConvertFmt(filename);
        files = new FileInformation[0];
        return DokanResult.NotImplemented;
    }

    #endregion DokanOperations member
}