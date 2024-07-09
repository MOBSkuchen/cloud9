using DokanNet;

namespace cloud9lib;

public interface IInstanceHandlerBlueprint : IDokanOperations
{
    public InstanceData ExposeInstanceData();
    public IClientBlueprint ExposeClient();

    public static System.IO.FileAccess ConvertFileAccess(DokanNet.FileAccess fileAccess)
    {
        return fileAccess switch
        { 
            DokanNet.FileAccess.ReadData => System.IO.FileAccess.Read, 
            DokanNet.FileAccess.WriteData => System.IO.FileAccess.Write,
            _ => System.IO.FileAccess.ReadWrite
        };
    }
}