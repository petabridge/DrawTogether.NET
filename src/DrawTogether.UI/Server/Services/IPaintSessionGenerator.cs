using System;

namespace DrawTogether.UI.Server.Services
{
    public interface IPaintSessionGenerator
    {
        /// <summary>
        /// Create a randomly named new drawing session.
        /// </summary>
        string CreateNewSession();
    }

    internal class GuidPaintSessionGenerator : IPaintSessionGenerator
    {
        public string CreateNewSession()
        {
            // will make the sessionId uri-friendly (and less ugly)
            return Akka.Util.Base64Encoding.Base64Encode(Guid.NewGuid().ToString());
        }
    }
}
