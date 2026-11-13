namespace DrawTogether.AppHost;

public class DrawTogetherConfiguration
{
    /// <summary>
    /// Whether to use persistent volumes for our database.
    /// </summary>
    /// <remarks>
    /// Defaults to false so we don't accidentally persist data during testing.
    /// </remarks>
    public bool UseVolumes { get; set; } = false;
    
    /// <summary>
    /// When this is enabled, we will use Akka Management to manage our cluster.
    ///
    /// This means exposing an additional internal endpoint and spinning up Azure storage.
    /// </summary>
    public bool UseAkkaManagement { get; set; } = true;

    /// <summary>
    /// The total number of replicas we're going to run in our cluster.
    ///
    /// Only relevant when <see cref="UseAkkaManagement"/> is enabled.
    /// </summary>
    public int Replicas { get; set; } = 1;

    /// <summary>
    /// Optional. A certificate folder (or a single .pfx file) for Akka.Remote TLS.
    /// When set, TLS is turned on for every DrawTogether replica. Off by default.
    /// </summary>
    public string? TlsCertificatePath { get; set; }

    /// <summary>
    /// Password for the PKCS#12 files in <see cref="TlsCertificatePath"/>.
    /// </summary>
    public string? TlsCertificatePassword { get; set; }
}