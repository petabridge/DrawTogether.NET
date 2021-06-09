namespace DrawTogether.UI.Shared.Connectivity
{
    public static class PaintSessionProtocol
    {
        public interface IPaintSessionMessage
        {
            /// <summary>
            /// Refers to a unique painting instance.
            /// </summary>
            string InstanceId { get; }
        }

        public sealed class JoinPaintSession : IPaintSessionMessage
        {
            public JoinPaintSession(string instanceId)
            {
                InstanceId = instanceId;
            }

            public string InstanceId { get; }
        }
    }
}
