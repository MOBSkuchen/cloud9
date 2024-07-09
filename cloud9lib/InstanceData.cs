namespace cloud9lib;

public struct InstanceData
{
    public InstanceData(String host, String username, int port, String password, bool isKeyAuth, String remotePath, 
        String driveName, String mountPath, double protocolVersion, String method) {
        Host = host;
        Username = username;
        Port = port;

        Password = password;
        IsKeyAuth = isKeyAuth;

        RemotePath = remotePath;
        DriveName = driveName;
        MountPath = mountPath;

        ProtocolVersion = protocolVersion;
        Method = method;
    }
    
    public String Host;
    public String Username;
    public int Port;

    public String Password;
    public bool IsKeyAuth;

    public String RemotePath;
    public String DriveName;
    public String MountPath;
    
    public double ProtocolVersion;
    public String Method;

    public static InstanceData? ConvertToInstanceData(Dictionary<string, string> instanceDataDictionary) {
        return new InstanceData(instanceDataDictionary["host"], instanceDataDictionary["username"],
            Convert.ToInt32(instanceDataDictionary["port"]), instanceDataDictionary["password"],
            instanceDataDictionary["isKeyAuth"] == "true", instanceDataDictionary["remotePath"],
            instanceDataDictionary["driveName"],
            instanceDataDictionary["mountPath"], 1.0, 
            instanceDataDictionary["method"]);
    }
}