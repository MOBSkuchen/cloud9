using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using cloud9lib;

namespace cloud9service;

public enum ErrorCodes
{
    Alright,
    InvalidConfig,
    CorruptedConfig,
    UnknownMethod,
    Unable2Start
}

public static class _
{
    public static Dictionary<string, string?> ToDictionary(this NameValueCollection nvc) => 
        nvc.AllKeys.ToDictionary(k => k, k => nvc[k]);
    
    public static String GetTimestamp(this DateTime value)
    {
        return value.ToString("dd.MM.yyyy;HH:mm:ss.ffff");
    }
}

public class InstanceManager
{
    private bool _closeRef;
    private readonly Task _runningTask;
    private InstanceHandler _instanceHandler;
    public InstanceData InstanceData;
    public static Random Random = new Random();
    public String StartedAt = DateTime.Now.GetTimestamp();

    public string Uid;

    public StatusResponse StatusResponse = new StatusResponse();

    public InstanceManager(InstanceData instanceData, InstanceHandler instHandler, string? uid = null)
    {
        if (uid != null) Uid = uid;
        else Uid = GetUid();
        InstanceData = instanceData;
        _instanceHandler = instHandler;
        _runningTask = Task.Run(() => Instance.CreateClientInstance(instHandler, ref _closeRef, StatusResponse));
    }

    public void Close()
    {
        _closeRef = true;
        _runningTask.Wait();
    }
    
    private string GetUid()
    {
        StringBuilder sb = new StringBuilder();
        foreach (byte b in GetUidBytes())
            sb.Append(b.ToString("X2"));
        return sb.ToString();
    }

    private byte[] GetUidBytes()
    {
        return SHA256.HashData(
            Encoding.UTF8.GetBytes($"{InstanceData.Host}:{InstanceData.Port}" + $"{InstanceData.Method}" +
        $"{InstanceData.RemotePath}{InstanceData.MountPath}{InstanceData.DriveName}" +
        $"{InstanceData.IsKeyAuth}{InstanceData.Password}{InstanceData.Username}" +
        $"{InstanceData.ProtocolVersion}{RandomString(15)}"));
    }
    
    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Next(s.Length)]).ToArray());
    }

    public void Save(bool autostart)
    {
        var filename = autostart ? $"A-{Uid}.json" : $"S-{Uid}.json";
        File.WriteAllBytes(filename, JsonSerializer.SerializeToUtf8Bytes(InstanceData.ConvertToDictionary(), JsonSerializerOptions.Default));
    }
}

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    public bool Shutdown;

    public Worker(IHostApplicationLifetime hostApplicationLifetime, ILogger<Worker> logger)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }
    
    public static HttpListener Listener;
    public static string Url = "http://localhost:4994/";

    public Dictionary<string, InstanceManager> Instances = new ();
    
    public async Task WriteRawResponse(HttpListenerResponse resp, byte[] data)
    {
        resp.ContentEncoding = Encoding.UTF8;
        resp.ContentLength64 = data.LongLength;
        await resp.OutputStream.WriteAsync(data, 0, data.Length);
        resp.Close();
    }

    public async Task WriteStringResponse(HttpListenerResponse resp, string data)
    {
        resp.ContentType = "text/plain";
        await WriteRawResponse(resp, Encoding.UTF8.GetBytes(data));
    }

    public async Task WriteJsonResponse(HttpListenerResponse resp, Dictionary<string, string> json)
    {
        resp.ContentType = "application/json";
        var data = JsonSerializer.Serialize(json);
        await WriteRawResponse(resp, Encoding.UTF8.GetBytes(data));
    }
    
    public bool AssertHas(String[] fields, Dictionary<string, string>? dict)
    {
        if (dict == null) return true;
        foreach (var field in fields)
        {
            if (!dict.ContainsKey(field)) return true;
        }
        return false;
    }
    
    public (ErrorCodes, string?) SpawnClient(Dictionary<string, string> configLoaded, string? uid = null)
    {
        if (AssertHas(
                new string[]
                {
                    "method", "remoteHost", "username", "password",
                    "port", "isKeyAuth", "driveName", "remotePath", "mountPath"     // Change host to remoteHost because of HTTP conflict
                }, configLoaded))
            return (ErrorCodes.InvalidConfig, null);
        
        configLoaded.Add("host", configLoaded["remoteHost"]);
        configLoaded.Remove("remoteHost");

        var instanceData = InstanceData.ConvertToInstanceData(configLoaded);
        if (instanceData == null) return (ErrorCodes.CorruptedConfig, null);
        
        IClientBlueprint? client;
        switch (instanceData.Value.Method.ToLower())
        {
            case "sftp":
            {
                client = new SftpDriver(instanceData.Value);
                break;
            }
            case "clone":
            {
                client = new CloneDriver(instanceData.Value);
                break;
            }
            default:
            {
                return (ErrorCodes.UnknownMethod, null);
            }
        }
        
        var fileManagement = new CloneFileManagement();
        
        var instHandler = new InstanceHandler(instanceData.Value, client, fileManagement);
        
        fileManagement.DepositInstanceHandler(instHandler);

        var manager = new InstanceManager(instanceData.Value, instHandler, uid);
        Instances.Add(manager.Uid, manager);
        
        Thread.Sleep(1000);
        if (!manager.StatusResponse.IsOk) return (ErrorCodes.Unable2Start, manager.StatusResponse.ErrMsg);
        
        return (ErrorCodes.Alright, manager.Uid);
    }

    private async void RespondWrongMethod(HttpListenerResponse resp)
    {
        resp.StatusCode = 405;
        await WriteStringResponse(resp, "Invalid Method");
    }

    private (ErrorCodes, string?) SpawnClientByFilename(string instance) =>
        SpawnClient(JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(instance))!,
            instance.Substring(2, instance.Length - 2));

    private void StartSavedAutostartInstances()
    {
        var files = Directory.EnumerateFiles("/").Where(f => f.StartsWith("A-") && f.EndsWith(".json"));

        foreach (var instance in files)
        {
            var client = SpawnClientByFilename(instance);
            if (client.Item1 != ErrorCodes.Alright) 
                _logger.LogError("Encountered error when starting autostart instance: {erc}", client.Item1);
        }
    }

    private List<string> GetAllInstances()
    {
        var savedInstances = Directory.EnumerateFiles(".").Where(f => f.StartsWith(".\\S-") && f.EndsWith(".json"));
        var savedAutostartInstances = Directory.EnumerateFiles(".").Where(f => f.StartsWith(".\\A-") && f.EndsWith(".json"));

        List<string> reportedInstances = new List<string>();

        foreach (var savedInstance in savedInstances)
        {
            reportedInstances.Add(SavedName2Uid(savedInstance));
        }
                
        foreach (var savedAutostartInstance in savedAutostartInstances)
        {
            reportedInstances.Add(SavedName2Uid(savedAutostartInstance));
        }

        foreach (var instance in Instances)
        {
            if (!instance.Value.StatusResponse.IsOk)
            {
                Instances.Remove(instance.Key);
            } else reportedInstances.Add(instance.Value.Uid);
        }
        
        return reportedInstances;
    }

    private Dictionary<string, string>? LoadSavedInstance(string filename)
    {
        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(filename), JsonSerializerOptions.Default);
    }

    private string SavedName2Uid(string filename) => filename.Substring(4, filename.Length - 4).Substring(0, filename.Length - 9);
    private bool MatchesUid(string uid, string filename) => SavedName2Uid(filename) == uid;

    public async Task SendSavedInstance(HttpListenerResponse resp, string savedInstance, bool running, bool autostart)
    {
        var instCfg = LoadSavedInstance(savedInstance)!;
        instCfg["running"] = running.ToString();
        instCfg["autostart"] = autostart.ToString();
        instCfg["startedAt"] = "null";
        instCfg["saved"] = "true";
                    
        resp.StatusCode = 200;
        await WriteJsonResponse(resp, instCfg);
    }

    public async Task HandleIncomingConnections()
    {

        // While a user hasn't visited the `shutdown` url, keep on handling requests
        while (!Shutdown)
        {
            // Will wait here until we hear from a connection
            HttpListenerContext ctx = await Listener.GetContextAsync();

            // Peel out the requests and response objects
            HttpListenerRequest req = ctx.Request;
            HttpListenerResponse resp = ctx.Response;
            
            if (req.HttpMethod == "OPTIONS") resp.AddHeader("Access-Control-Allow-Headers", "*");
            resp.AppendHeader("Access-Control-Allow-Origin", "*");

            // Print out some info about the request
            _logger.LogInformation("{userHostName} -> {method} > {url}", req.UserHostName, req.HttpMethod, req.Url);

            if (!req.IsLocal)
            {
                // Forbidden
                _logger.LogWarning("Forbidden Request from {addr} (outside)", req.UserHostAddress);
                resp.StatusCode = 403;
                await WriteStringResponse(resp, "Forbidden, only local requests are allowed!");
                return;
            }

            if (req.Url?.AbsolutePath == "/create-instance")
            {
                if (req.HttpMethod != "POST")
                {
                    RespondWrongMethod(resp);
                    return;
                }
                
                var client = SpawnClient(req.Headers.ToDictionary()!);
                
                if (client.Item1 == ErrorCodes.Unable2Start)
                {
                    resp.StatusCode = 500;
                    await WriteStringResponse(resp, client.Item2!);
                    return;
                }
                
                if (client.Item1 != ErrorCodes.Alright)
                {
                    resp.StatusCode = 400;
                    await WriteStringResponse(resp, client.Item1.ToString());
                    return;
                }
                
                resp.StatusCode = 200;
                if (client.Item2 == null) client.Item2 = "";
                await WriteStringResponse(resp, client.Item2);
                return;
            }

            if (req.Url!.AbsolutePath.StartsWith("/instance/") && req.Url.AbsolutePath.EndsWith("/stop"))
            {
                if (req.HttpMethod != "POST")
                {
                    RespondWrongMethod(resp);
                    return;
                }

                var instanceName = req.Url!.AbsolutePath.Substring(10, req.Url!.AbsolutePath.Length - 15);
                if (!Instances.ContainsKey(instanceName))
                {
                    resp.StatusCode = 400;
                    await WriteStringResponse(resp, "Unknown");
                    return;
                }

                var instance = Instances[instanceName];
                instance.Close();
                Instances.Remove(instanceName);
                
                resp.StatusCode = 200;
                await WriteStringResponse(resp, instanceName);
                return;
            }
            
            if (req.Url!.AbsolutePath.StartsWith("/instance/") && req.Url.AbsolutePath.EndsWith("/save"))
            {
                if (req.HttpMethod != "POST")
                {
                    RespondWrongMethod(resp);
                    return;
                }

                var instanceName = req.Url!.AbsolutePath.Substring(10, req.Url!.AbsolutePath.Length - 15);
                if (!Instances.ContainsKey(instanceName))
                {
                    resp.StatusCode = 400;
                    await WriteStringResponse(resp, "Unknown");
                    return;
                }

                var headers = req.Headers.ToDictionary();

                var instance = Instances[instanceName];
                instance.Save(headers.ContainsKey("autostart") && headers["autostart"] == "true");
                
                resp.StatusCode = 200;
                await WriteStringResponse(resp, instanceName);
                return;
            }

            if (req.Url!.AbsolutePath == "/wipe")
            {
                if (req.HttpMethod != "POST")
                {
                    RespondWrongMethod(resp);
                    return;
                }

                var amount = Instances.Keys.Count;

                foreach (var instance in Instances)
                {
                    instance.Value.Close();
                    Instances.Remove(instance.Key);
                }
                
                resp.StatusCode = 200;
                await WriteStringResponse(resp, amount.ToString());
                return;
            }

            if (req.Url?.AbsolutePath == "/shutdown")
            {
                if (req.HttpMethod != "POST")
                {
                    RespondWrongMethod(resp);
                    return;
                }
                Shutdown = true;
                resp.StatusCode = 200;
                await WriteStringResponse(resp, "bye");
                return;
            }
            
            if (req.Url?.AbsolutePath == "/instances")
            {
                if (req.HttpMethod != "GET")
                {
                    RespondWrongMethod(resp);
                    return;
                }
                
                resp.StatusCode = 200;
                await WriteStringResponse(resp, string.Join("\n", GetAllInstances()));
                return;
            }

            if (req.Url!.AbsolutePath.StartsWith("/instance/"))
            {
                if (req.HttpMethod != "GET")
                {
                    RespondWrongMethod(resp);
                    return;
                }
                
                var instanceName = req.Url!.AbsolutePath.Substring(10, req.Url!.AbsolutePath.Length - 10);
                var savedInstances = Directory.EnumerateFiles(".").Where(f => f.StartsWith(".\\S-") && f.EndsWith(".json"));
                var savedAutostartInstances = Directory.EnumerateFiles(".").Where(f => f.StartsWith(".\\A-") && f.EndsWith(".json"));

                var running = Instances.ContainsKey(instanceName);

                foreach (var instance in Instances)
                {
                    if (!instance.Value.StatusResponse.IsOk)
                    {
                        Instances.Remove(instance.Key);
                    }
                }

                foreach (var savedInstance in savedInstances)
                {
                    if (MatchesUid(instanceName, savedInstance))
                    {
                        await SendSavedInstance(resp, savedInstance, running, true);
                        return;
                    }
                }

                foreach (var savedAutostartInstance in savedAutostartInstances)
                {
                    if (MatchesUid(instanceName, savedAutostartInstance))
                    {
                        await SendSavedInstance(resp, savedAutostartInstance, running, true);
                        return;
                    }
                }

                if (Instances.ContainsKey(instanceName))
                {
                    var instCfg = Instances[instanceName].InstanceData.ConvertToDictionary();
                    instCfg["running"] = "true";
                    instCfg["autostart"] = "false";
                    instCfg["saved"] = "false";
                    instCfg["startedAt"] = Instances[instanceName].StartedAt;
                    
                    resp.StatusCode = 200;
                    await WriteJsonResponse(resp, instCfg);
                    return;
                }
                
                resp.StatusCode = 400;
                await WriteStringResponse(resp, "Unknown");
                return;
            }

            if (req.Url.AbsolutePath == "/test/error")
            {
                resp.StatusCode = 200;
                await WriteStringResponse(resp, "Throwing error now...");
                throw new Exception("Test error from request");
            }

            resp.StatusCode = 404;
            await WriteStringResponse(resp, "Not found / Invalid Method");
            return;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Listener = new HttpListener();
        Listener.Prefixes.Add(Url);
        Listener.Start();
        _logger.LogInformation("Listening for connections on {0}", Url);
        
        StartSavedAutostartInstances();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (Shutdown) break;
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            try
            {
                await HandleIncomingConnections();
            }
            catch (Exception e)
            {
                _logger.LogError("Got error {err} \n[from cloud9service.Worker.HandleIncomingConnections()]", e);
            }
        }
        _logger.LogInformation("Closing");
        Listener.Close();
        _hostApplicationLifetime.StopApplication();
    }
}