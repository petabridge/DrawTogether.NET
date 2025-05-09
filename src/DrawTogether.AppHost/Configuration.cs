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
}