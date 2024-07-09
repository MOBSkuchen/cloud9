using DokanNet;
using DokanNet.Logging;

namespace cloud9lib;

public class Instance
{
    public static void CreateClientInstance(IInstanceHandlerBlueprint instanceHandler)
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
                
                var dokanBuilder = new DokanInstanceBuilder(dokan)
                    .ConfigureOptions(options =>
                    {
                        options.Options = DokanOptions.StderrOutput;
                        options.MountPoint = instanceHandler.ExposeInstanceData().MountPath;
                    });
                using (var dokanInstance = dokanBuilder.Build(instanceHandler))
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