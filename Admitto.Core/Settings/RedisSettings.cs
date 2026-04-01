namespace Admitto.Core.Settings
{
    public class RedisSettings
    {
        /// <summary>Accepted values: "Standalone" | "Sentinel"</summary>
        public string Mode { get; set; } = "Standalone";
        public string SentinelServiceName { get; set; } = "mymaster";
    }
}
