using DokanNet;
using DokanNet.Logging;

namespace cloud9client;

public class Instance
{
    public static void CreateClientInstance(InstanceData instanceData, IClientBlueprint clientBlueprint)
    {
        try
        {
            using (var mre = new ManualResetEvent(false))
            using (var dokanLogger = new ConsoleLogger("[System] "))
            using (var dokan = new Dokan(dokanLogger))
            {
                Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) =>
                {
                    e.Cancel = true;
                    mre.Set();
                };

                var rfs = new InstanceHandler(instanceData, clientBlueprint);
                
                var dokanBuilder = new DokanInstanceBuilder(dokan)
                    .ConfigureOptions(options =>
                    {
                        options.Options = DokanOptions.DebugMode | DokanOptions.StderrOutput;
                        options.MountPoint = instanceData.MountPath;
                    });
                using (var dokanInstance = dokanBuilder.Build(rfs))
                {
                    mre.WaitOne();
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