using DokanNet;
using DokanNet.Logging;

namespace cloud9lib;

public class Instance
{
    public static void CreateClientInstance(IInstanceHandlerBlueprint instanceHandler, ref bool closeRef)
    {
        try
        {
            using (var mre = new ManualResetEvent(false))
            using (var dokanLogger = new ConsoleLogger("[System] "))
            using (var dokan = new Dokan(dokanLogger))
            {
                var dokanBuilder = new DokanInstanceBuilder(dokan)
                    .ConfigureOptions(options =>
                    {
                        options.Options = DokanOptions.StderrOutput;
                        options.MountPoint = instanceHandler.ExposeInstanceData().MountPath;
                    });
                using (var dokanInstance = dokanBuilder.Build(instanceHandler))
                {
                    var handle = mre.GetSafeWaitHandle();
                    while (!closeRef) { }
                    handle.Close();
                }
                Console.WriteLine(@"Success");
            }
        }
        catch (DokanException ex)
        {
            Console.WriteLine(@"Error: " + ex.Message);
        }
    }
}