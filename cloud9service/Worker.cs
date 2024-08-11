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
    private bool _closeRef = false;
    private readonly Task _runningTask;
    private InstanceHandler _instanceHandler;
    public InstanceData InstanceData;
    public static Random Random = new Random();
    public String StartedAt = DateTime.Now.GetTimestamp();

    public InstanceManager(InstanceData instanceData, InstanceHandler instHandler)
    {
        InstanceData = instanceData;
        _instanceHandler = instHandler;
        _runningTask = Task.Run(() => Instance.CreateClientInstance(instHandler, ref _closeRef));
    }

    public void Close()
    {
        _closeRef = true;
        _runningTask.Wait();
    }

    public string GetUid()
    {
        return Encoding.UTF8.GetString(SHA256.HashData(
            Encoding.UTF8.GetBytes($"{InstanceData.Host}:{InstanceData.Port}" + $"{InstanceData.Method}" +
        $"{InstanceData.RemotePath}{InstanceData.MountPath}{InstanceData.DriveName}" +
        $"{InstanceData.IsKeyAuth}{InstanceData.Password}{InstanceData.Username}" +
        $"{InstanceData.ProtocolVersion}{RandomString(15)}")));
    }
    
    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Next(s.Length)]).ToArray());
    }
}

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    public bool Shutdown = false;

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
    
    public (ErrorCodes, string?) SpawnClient(Dictionary<string, string> configLoaded)
    {
        if (AssertHas(
                new string[]
                {
                    "method", "host", "username", "password",
                    "port", "isKeyAuth", "driveName", "remotePath", "mountPath"
                }, configLoaded))
            return (ErrorCodes.InvalidConfig, null);

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

        var manager = new InstanceManager(instanceData.Value, instHandler);
        var uid = manager.GetUid();
        Instances.Add(uid, manager);
        
        return (ErrorCodes.Alright, uid);
    }

    private async void RespondWrongMethod(HttpListenerResponse resp)
    {
        resp.StatusCode = 405;
        await WriteStringResponse(resp, "Invalid Method");
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

            if (req.Url?.AbsolutePath == "/start-instance")
            {
                if (req.HttpMethod != "POST")
                {
                    RespondWrongMethod(resp);
                    return;
                }
                var client = SpawnClient(req.Headers.ToDictionary()!);
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
                await WriteStringResponse(resp, string.Join("\n", Instances.Keys));
                return;
            }

            if (req.Url!.AbsolutePath.StartsWith("/instances/"))
            {
                if (req.HttpMethod != "GET")
                {
                    RespondWrongMethod(resp);
                    return;
                }
                var instanceName = req.Url!.AbsolutePath.Substring(0, 11);
                if (!Instances.ContainsKey(instanceName))
                {
                    resp.StatusCode = 400;
                    await WriteStringResponse(resp, "Unknown");
                    return;
                }

                resp.StatusCode = 200;
                var dict = Instances[instanceName].InstanceData.ConvertToDictionary();
                dict["startedAt"] = Instances[instanceName].StartedAt;
                await WriteJsonResponse(resp, dict);
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