using System.Net;
using Akka.Cluster.Hosting;
using Akka.Remote.Hosting;
using DrawTogether.Actors;
using Petabridge.Cmd.Host;

namespace DrawTogether.Config;

public class AkkaSettings
{
    public string ActorSystemName { get; set; } = "DrawTogether";

    public bool LogConfigOnStart { get; set; } = false;

    public RemoteOptions RemoteOptions { get; set; } = new()
    {
        // can be overridden via config, but is dynamic by default
        PublicHostName = Dns.GetHostName(),
        HostName = "0.0.0.0",
        Port = 8081
    };

    public ClusterOptions ClusterOptions { get; set; } = new ClusterOptions()
    {
        // use our dynamic local host name by default
        SeedNodes = [$"akka.tcp://DrawTogether@{Dns.GetHostName()}:8081"],
        Roles = [ClusterConstants.DrawStateRoleName]
    };

    public ShardOptions ShardOptions { get; set; } = new ShardOptions();
    
    public AkkaManagementOptions? AkkaManagementOptions { get; set; }

    public PetabridgeCmdOptions PbmOptions { get; set; } = new()
    {
        Host = "0.0.0.0", 
        Port = 9110
    };
}