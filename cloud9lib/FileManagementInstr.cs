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
    { return _instanceHandler.ExposeClient().ConvertFmt(originalFileName); }

    public string GenerateDirName(string originalDirName)
    { return _instanceHandler.ExposeClient().ConvertFmt(originalDirName); }

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
    public static (string FileName, string FileExtension) GetFileNameAndExtension(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        filePath = filePath.Split("/")[-1];
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string fileExtension = Path.GetExtension(filePath);

        return (fileName, fileExtension);
    }
    
    public string GenerateFileName(string originalFileName)
    {
        (string fileName, string fileExtension) = GetFileNameAndExtension(originalFileName);
        return $"{fileExtension}/{fileName}";
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