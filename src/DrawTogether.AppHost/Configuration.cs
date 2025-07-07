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

    public DeployEnvironment DeployEnvironment { get; set; } = DeployEnvironment.Local;
}