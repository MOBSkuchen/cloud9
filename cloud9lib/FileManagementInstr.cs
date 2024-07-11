using DokanNet;
using Microsoft.VisualBasic;

namespace cloud9lib;

public interface IFileManagementInstr
{
    public string GenerateFileName(string originalFileName);
    public string GenerateDirName(string originalDirName);

    public List<FileInformation> ListFiles(string path);
    public List<FileInformation> FindFilesWithPattern(string path);

    public (bool, bool) GetAllowCreate();
    
    public (bool, bool) GetAllowMove();
    
    public bool GetAllowWrite();
    
    public (bool, bool) GetAllowDelete();
    
    public void DepositInstanceHandler(IInstanceHandlerBlueprint instanceHandler);
}

public class CloneFileManagement : IFileManagementInstr
{
    private IInstanceHandlerBlueprint _instanceHandler;
    
    public string GenerateFileName(string originalFileName)
    { return originalFileName; }

    public string GenerateDirName(string originalDirName)
    { return originalDirName; }

    public List<FileInformation> ListFiles(string path)
    { return _instanceHandler.ExposeClient().ListFiles(path); }

    public List<FileInformation> FindFilesWithPattern(string path)
    { return _instanceHandler.ExposeClient().PatternSearch(path); }
    
    public (bool, bool) GetAllowCreate() { return (true, true); }

    public (bool, bool) GetAllowMove() { return (true, true); }

    public bool GetAllowWrite() { return true; }

    public (bool, bool) GetAllowDelete() { return (true, true); }

    public void DepositInstanceHandler(IInstanceHandlerBlueprint instanceHandler)
    { _instanceHandler = instanceHandler; }
}

public class ExtensionSortedFileManagement : IFileManagementInstr
{
    private IInstanceHandlerBlueprint _instanceHandler;
    
    public string GenerateFileName(string originalFileName)
    {
        var fileExt = originalFileName.Split("/", 1);
        var filename = fileExt[1];
        var ext = fileExt[0];
        return $"{filename}.{ext}";
    }

    public string GenerateDirName(string originalDirName)
    {
        return String.Empty;
    }

    public List<FileInformation> ListFiles(string path)
    {
        return _instanceHandler.ExposeClient().ListFiles(Path.GetExtension(path));
    }

    public List<FileInformation> FindFilesWithPattern(string path)
    {
        return _instanceHandler.ExposeClient().PatternSearch(path);
    }

    public (bool, bool) GetAllowCreate()
    {
        return (true, false);
    }

    public (bool, bool) GetAllowMove()
    {
        return (false, false);
    }

    public bool GetAllowWrite()
    {
        return true;
    }

    public (bool, bool) GetAllowDelete()
    {
        return (true, false);
    }

    public void DepositInstanceHandler(IInstanceHandlerBlueprint instanceHandler)
    {
        _instanceHandler = instanceHandler;
    }
}