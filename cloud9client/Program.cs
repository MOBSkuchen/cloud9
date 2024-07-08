using Newtonsoft.Json;

namespace cloud9client;

class Program
{
    public static void AssertExpression(bool expr)
    {
        if (!expr) Error(5, "Assert failed!");;
    }

    public static void AssertHas(String[] fields, Dictionary<string, string>? dict)
    {
        if (dict == null)
        {
            Error(6, "Invalid config file!");
            return;
        }
        foreach (var field in fields)
        {
            if (!dict.ContainsKey(field)) Error(6, "Invalid config file!");
        }
    }

    public static void Error(int err, String msg)
    {
        Console.WriteLine($"Error ({err}) : {msg}");
        Environment.Exit(err);
    }

    public static Dictionary<string, string>? LoadConfig(String path)
    {
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
    }

    public static SftpDriver CreateSftpClient(InstanceData instanceData)
    {
        return new SftpDriver(instanceData);
    }

    public static CloneDriver CreateCloneClient(InstanceData instanceData)
    {
        return new CloneDriver(instanceData);
    }

    public static void SpawnClient(String configFilePath)
    {
        AssertExpression(File.Exists(configFilePath));

        var configLoaded = LoadConfig(configFilePath);
        AssertHas(new string[] {"method", "host", "username", "password", "port", "isKeyAuth", "driveName", "remotePath", "mountPath"}, configLoaded);

        var instanceData = InstanceData.ConvertToInstanceData(configLoaded);
        if (instanceData == null) Error(10, "Corrupted config file");
        
        IClientBlueprint? client = null;
        switch (configLoaded["method"].ToLower())
        {
            case "sftp":
            {
                client = CreateSftpClient(instanceData.Value);
                break;
            }
            case "sock":
            {
                Error(404, "Not implemented / found");
                break;
            }
            case "clone":
            {
                client = CreateCloneClient(instanceData.Value);
                break;
            }
            default:
            {
                Error(9, "Unknown method");
                break;
            }
        }
        if (client == null) return;
        
        Instance.CreateClientInstance(instanceData.Value, client);
    }
    
    public static void Error(int err) {Error(err, "No message provided.");}
    
    public static void Main(String[] args)
    {
        Console.Title = "Cloud9Client";
        String version = "1.0";
        
        if (args.Length == 0) {Error(2, "Not enough arguments!");}

        var exc = args[0];

        switch (exc)
        {
            case "client":
                if (args.Length > 1) SpawnClient(args[1]);
                else Error(2, "Not enough arguments!");
                break;
            case "version":
                Console.WriteLine($"Version {version}");
                break;
            case "help":
                Console.WriteLine($"Cloud9 CLI Client (version {version})");
                Console.WriteLine("help - Get help");
                Console.WriteLine("version - Get version");
                Console.WriteLine("client <configfile> - Spawn a new client instance");
                Console.WriteLine("server <configfile> - Spawn a new server instance");
                Console.WriteLine("End of help");
                Console.WriteLine("------------");
                Console.WriteLine("Allowed client / server methods");
                Console.WriteLine(" + SFTP (client only)     : Connects to running SFTP server");
                Console.WriteLine(" [COMING SOON] - SOCK (client & server) : Connects to running Cloud9 server");
                Console.WriteLine("------------");
                break;
            default:
                Error(3, "Invalid exc command");
                break;
        }
        
        Console.WriteLine("Done");
    }
}