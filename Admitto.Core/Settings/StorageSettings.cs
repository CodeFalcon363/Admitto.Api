namespace Admitto.Core.Settings
{
    public class StorageSettings
    {
        /// <summary>Accepted values: "Local" | "S3"</summary>
        public string Provider { get; set; } = "S3";
    }
}
