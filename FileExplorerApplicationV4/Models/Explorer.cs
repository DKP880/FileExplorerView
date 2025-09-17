using System.Text.Json.Serialization;

namespace FileExplorer.Models
{
    public class Explorer
    {
        // Common properties
        [JsonPropertyName("Provider")]
        public string Provider { get; set; } = string.Empty;

        // Local Provider Properties
        [JsonPropertyName("ExplorerName")]
        public string? ExplorerName { get; set; }
        [JsonPropertyName("RootPaths")]
        public List<string>? RootPaths { get; set; }

        // FTP/SFTP Provider Properties
        [JsonPropertyName("Host")]
        public string? Host { get; set; }
        [JsonPropertyName("Port")]
        public int? Port { get; set; }
        [JsonPropertyName("Username")]
        public string? Username { get; set; }
        [JsonPropertyName("Password")]
        public string? Password { get; set; }
        [JsonPropertyName("RemoteDir")]
        public string? RemoteDir { get; set; }
        [JsonPropertyName("Type")]
        public string? Type { get; set; }
        [JsonPropertyName("LastUsed")]
        public DateTime? LastUsed { get; set; }
    }
}