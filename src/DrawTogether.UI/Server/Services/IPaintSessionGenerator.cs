namespace DrawTogether.UI.Server.Services;

public interface IPaintSessionGenerator
{
    /// <summary>
    ///     Create a randomly named new drawing session.
    /// </summary>
    string CreateNewSession();
}

internal class GuidPaintSessionGenerator : IPaintSessionGenerator
{
    public string CreateNewSession()
    {
        // return a base64 encoded GUID
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}