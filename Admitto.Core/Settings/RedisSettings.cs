namespace Admitto.Core.Settings
{
    public class RedisSettings
    {
        /// <summary>Accepted values: "Standalone" | "Sentinel"</summary>
        public string Mode { get; set; } = "Standalone";
        public string SentinelServiceName { get; set; } = "mymaster";
        /// <summary>Set false in development so a missing Redis server doesn't crash startup.</summary>
        public bool AbortOnConnectFail { get; set; } = true;
    }
}
