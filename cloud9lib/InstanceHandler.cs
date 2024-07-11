using System.Security.AccessControl;
using DokanNet;
using FileAccess = DokanNet.FileAccess;

namespace cloud9lib;

public class InstanceHandler : IInstanceHandlerBlueprint
{
    private readonly IClientBlueprint _clientHandler;
    private readonly InstanceData _instanceData;
    private readonly IFileManagementInstr _fileManagement;
    
    #region DokanOperations member

    public InstanceHandler(InstanceData instanceData, IClientBlueprint clientHandler, IFileManagementInstr fileManagement)
    {
        _clientHandler = clientHandler;
        _instanceData = instanceData;
        _fileManagement = fileManagement;
    }

    public InstanceData ExposeInstanceData() { return _instanceData; }

    public IClientBlueprint ExposeClient() { return _clientHandler; }

    public IFileManagementInstr ExposeFileManagement()
    {
        return _fileManagement;
    }
    
    public void Cleanup(string filename, IDokanFileInfo info)
    {
        _clientHandler.CloseHandle(info.Context);
    }

    public void CloseFile(string filename, IDokanFileInfo info)
    {
        _clientHandler.CloseHandle(info.Context);
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
        if (mode is FileMode.CreateNew or FileMode.Create) {
            if (info.IsDirectory && _fileManagement.GetAllowCreate().Item2) _clientHandler.CreateDirectory(_fileManagement.GenerateDirName(filename));
            else if (_fileManagement.GetAllowCreate().Item1) _clientHandler.CreateFile(_fileManagement.GenerateFileName(filename)); }
        
        switch (_clientHandler.IsDirectory(_fileManagement.GenerateDirName(filename))) {
            case true:
                info.IsDirectory = true;
                return DokanResult.Success;
            case false when info.IsDirectory:
                return DokanResult.NotADirectory;
            default:
                info.Context = _clientHandler.GetFileStream(_fileManagement.GenerateFileName(filename), mode, IInstanceHandlerBlueprint.ConvertFileAccess(access), share);
                return DokanResult.Success;
        }
    }

    public NtStatus DeleteDirectory(string filename, IDokanFileInfo info) {
        _clientHandler.DeletePath(_fileManagement.GenerateDirName(filename));
        return DokanResult.Success;
    }

    public NtStatus DeleteFile(string filename, IDokanFileInfo info) {
        _clientHandler.DeletePath(_fileManagement.GenerateFileName(filename));
        return DokanResult.Success;
    }

    public NtStatus FlushFileBuffers(string filename, IDokanFileInfo info) {
        _clientHandler.FlushHandle(info.Context);
        return DokanResult.Success;
    }

    public NtStatus FindFiles(
        string filename,
        out IList<FileInformation> files,
        IDokanFileInfo info)
    {
        files = _fileManagement.ListFiles(filename);
        return DokanResult.Success;
    }

    public NtStatus GetFileInformation(
        string filename,
        out FileInformation fileinfo,
        IDokanFileInfo info)
    {
        fileinfo = _clientHandler.GetFileInfo(_fileManagement.GenerateFileName(filename));
        return DokanResult.Success;
    }

    public NtStatus MoveFile(
        string filename,
        string newname,
        bool replace,
        IDokanFileInfo info)
    {
        if (info.IsDirectory && _fileManagement.GetAllowMove().Item2) return DokanResult.AccessDenied;
        if (!info.IsDirectory && _fileManagement.GetAllowMove().Item1) return DokanResult.AccessDenied;
        
        _clientHandler.MoveFile(_fileManagement.GenerateFileName(filename), _fileManagement.GenerateFileName(newname));
        return DokanResult.Success;
    }

    public NtStatus ReadFile(
        string filename,
        byte[] buffer,
        out int readBytes,
        long offset,
        IDokanFileInfo info)
    {
        readBytes = _clientHandler.IoReadAction(info.Context, buffer, offset);
        return DokanResult.Success;
    }

    public NtStatus SetEndOfFile(string filename, long length, IDokanFileInfo info)
    {
        _clientHandler.SetIoLength(info.Context, length);
        return DokanResult.Success;
    }

    public NtStatus SetAllocationSize(string filename, long length, IDokanFileInfo info)
    {
        _clientHandler.SetIoLength(info.Context, length);
        return DokanResult.Success;
    }

    public NtStatus SetFileAttributes(
        string filename,
        FileAttributes attr,
        IDokanFileInfo info)
    {
        _clientHandler.SetFileAttributes(_fileManagement.GenerateFileName(filename), attr);
        return DokanResult.Success;
    }

    public NtStatus SetFileTime(
        string filename,
        DateTime? ctime,
        DateTime? atime,
        DateTime? mtime,
        IDokanFileInfo info)
    {
        _clientHandler.SetFileTimes(_fileManagement.GenerateFileName(filename), atime, mtime, ctime);
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
        writtenBytes = _clientHandler.IoWriteAction(info.Context, buffer, offset);
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
            security = _clientHandler.GetFileSystemSecurity(filename);
            return security == null ? DokanResult.Error : DokanResult.Success;
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
