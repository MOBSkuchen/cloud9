using DokanNet;
using DokanNet.Logging;

namespace cloud9lib;

public class StatusResponse
{
    public bool IsOk = true;
    public string? ErrMsg;
    public bool IsDone = false;
}

public class Instance
{
    public static void CreateClientInstance(IInstanceHandlerBlueprint instanceHandler, ref bool closeRef, StatusResponse? statusResponse = null)
    {
        try
        {
            using (var mre = new ManualResetEvent(false))
            {
                ILogger dokanLogger;
                dokanLogger = new NullLogger();
                using (var dokan = new Dokan(dokanLogger))
                {
                    var dokanBuilder = new DokanInstanceBuilder(dokan)
                        .ConfigureOptions(options =>
                        {
                            options.Options = statusResponse == null ? DokanOptions.StderrOutput : DokanOptions.AltStream;
                            options.MountPoint = instanceHandler.ExposeInstanceData().MountPath;
                        });
                    using (var dokanInstance = dokanBuilder.Build(instanceHandler))
                    {
                        var handle = mre.GetSafeWaitHandle();
                        while (!closeRef) { }

                        handle.Close();
                    }
                    if (statusResponse != null) statusResponse.IsDone = true;
                }
            }
        }
        catch (DokanException ex)
        {
            if (statusResponse != null)
            {
                statusResponse.IsOk = false;
                statusResponse.IsDone = true;
                statusResponse.ErrMsg = ex.Message;
            }
        }
    }
}