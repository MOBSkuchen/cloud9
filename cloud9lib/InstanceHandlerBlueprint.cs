using DokanNet;

namespace cloud9lib;

public interface IInstanceHandlerBlueprint : IDokanOperations
{
    public InstanceData ExposeInstanceData();
    public IClientBlueprint ExposeClient();
}