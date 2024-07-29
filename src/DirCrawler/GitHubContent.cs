using System.Text.Json.Serialization;

namespace DirCrawler
{
    public class GitHubContent
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }
    }
}
