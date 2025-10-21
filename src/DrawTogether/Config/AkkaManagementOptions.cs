using System.Net;

namespace DrawTogether.Config;


public class AkkaManagementOptions
{
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The hostname to use for Akka.Management HTTP endpoint.
    /// If not set, Akka.Management will auto-detect the IP address.
    /// For Kubernetes, leave this null to auto-detect the pod IP.
    /// For Aspire/Azure, set this to "localhost" or the configured hostname.
    /// </summary>
    public string? Hostname { get; set; } = null;

    public int Port { get; set; } = 8558;
    public string PortName { get; set; } = "management";

    public string ServiceName { get; set; } = "drawtogether";

    /// <summary>
    /// Determines the number of nodes we need to make contact with in order to form a cluster initially.
    ///
    /// 3 is a safe default value.
    /// </summary>
    public int RequiredContactPointsNr { get; set; } = 3;

    public DiscoveryMethod DiscoveryMethod { get; set; } = DiscoveryMethod.Config;
    
    /// <summary>
    /// Whether to filter contact points on the fallback port.
    /// Should be true for Kubernetes (fixed ports) and false for Aspire (dynamic ports).
    /// </summary>
    public bool FilterOnFallbackPort { get; set; } = true;
}

/// <summary>
/// Determines which Akka.Discovery method to use when discovering other nodes to form and join clusters.
/// </summary>
public enum DiscoveryMethod
{
    Config,
    Kubernetes,
    AwsEcsTagBased,
    AwsEc2TagBased,
    AzureTableStorage
}