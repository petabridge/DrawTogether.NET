using System.Net;
using System.Security.Cryptography.X509Certificates;
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

    public TlsSettings? TlsSettings { get; set; }
}

public class TlsSettings
{
    /// <summary>
    /// Enable or disable TLS for Akka.Remote communication
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Path to the certificate file (.pfx or .p12)
    /// </summary>
    public string? CertificatePath { get; set; }

    /// <summary>
    /// Password for the certificate file (if encrypted)
    /// </summary>
    public string? CertificatePassword { get; set; }

    /// <summary>
    /// Enable certificate validation (default: true)
    /// Set to false ONLY for testing with self-signed certificates
    /// </summary>
    public bool ValidateCertificates { get; set; } = true;

    /// <summary>
    /// Load the X509Certificate2 from the configured path and password
    /// </summary>
    public X509Certificate2? LoadCertificate()
    {
        if (string.IsNullOrWhiteSpace(CertificatePath))
            return null;

        if (!File.Exists(CertificatePath))
            throw new FileNotFoundException($"Certificate file not found at: {CertificatePath}");

        return !string.IsNullOrWhiteSpace(CertificatePassword)
            ? X509CertificateLoader.LoadPkcs12FromFile(CertificatePath, CertificatePassword)
            : X509CertificateLoader.LoadCertificateFromFile(CertificatePath);
    }
}