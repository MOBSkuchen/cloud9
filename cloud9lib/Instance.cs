using DokanNet;
using DokanNet.Logging;

namespace cloud9lib;

public class StatusResponse
{
    public bool IsOk = true;
    public string? ErrMsg;

    public List<string> Log = new List<string>();
}

public class CustomLogger(StatusResponse response) : ILogger
{
    public bool DebugEnabled { get; } = true;

    public void Debug(string message, params object[] args) =>
        response.Log.Add("DEBUG " + string.Format(message, args));

    public void Info(string message, params object[] args) => response.Log.Add("INFO " + string.Format(message, args));

    public void Warn(string message, params object[] args) => response.Log.Add("WARN " + string.Format(message, args));

    public void Error(string message, params object[] args) => response.Log.Add("ERROR " + string.Format(message, args));

    public void Fatal(string message, params object[] args) => response.Log.Add("FATAL " + string.Format(message, args));

}

public class Instance
{
    public static void CreateClientInstance(IInstanceHandlerBlueprint instanceHandler, ref bool closeRef, StatusResponse? statusResponse = null)
    {
        try
        {
            using (var mre = new ManualResetEvent(false))
            {
                ILogger dokanLogger = (statusResponse == null ? (new NullLogger()) : (new CustomLogger(statusResponse)));
                using (var dokan = new Dokan(dokanLogger))
                {
                    var dokanBuilder = new DokanInstanceBuilder(dokan)
                        .ConfigureOptions(options =>
                        {
                            options.Options = DokanOptions.AltStream;
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
        }
        catch (DokanException ex)
        {
            if (statusResponse != null)
            {
                statusResponse.IsOk = false;
            }
        }
    }
}