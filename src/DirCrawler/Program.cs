using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Diagnostics;

namespace DirCrawler
{
    public class Program
    {
        private async static Task Main(string[] args)
        {
            var fileLinks = new List<FileLink>();

            if (args.Length < 3)
            {
                Console.WriteLine("Specify the Repository name, Branch name, and Output directory.");
                return;
            }

            var repository = args[0];

            if (string.IsNullOrWhiteSpace(repository))
            {
                Console.WriteLine("Repository cannot be empty.");
                return;
            }

            var branch = args[1];

            if (string.IsNullOrWhiteSpace(branch))
            {
                Console.WriteLine("Branch name cannot be empty.");
                return;
            }

            var outputDir = args[2];

            if (string.IsNullOrWhiteSpace(outputDir))
            {
                Console.WriteLine("Output directory cannot be empty.");
                return;
            }

            var initUrl = $"{repository}/contents?ref={branch}";

            Console.WriteLine(initUrl);

            var stopwatch = new Stopwatch();

            stopwatch.Start();
            await RecordLinksAndPathsAsync(fileLinks, initUrl);
            stopwatch.Stop();

            TimeSpan timeTaken = stopwatch.Elapsed;
            string timeFormatted = string.Format("{0:D2}m:{1:D2}s:{2:D3}ms",
                timeTaken.Minutes,
                timeTaken.Seconds,
                timeTaken.Milliseconds);

            Console.WriteLine(timeFormatted);

            string linksJson = JsonSerializer.Serialize(fileLinks);
            File.WriteAllText($"{outputDir}/lib.json", linksJson);
        }

        private static async Task RecordLinksAndPathsAsync(List<FileLink> fileLinks, string url)
        {
            using var httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://api.github.com/repos/"),
            };
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("Request");
            httpClient.DefaultRequestHeaders.Authorization = new ("Bearer", Settings.Token);

            using HttpResponseMessage response = await httpClient.GetAsync(url);
            string jsonResponse = await response.Content.ReadAsStringAsync();

            var contents = JsonSerializer.Deserialize<List<GitHubContent>>(jsonResponse);

            foreach (var item in contents)
            {
                if (item.DownloadUrl is not null)
                {
                    string directoryPath = GetDirectoryPath(item.Path);
                    var fileLink = new FileLink(item.Name, directoryPath, item.DownloadUrl);
                    fileLinks.Add(fileLink);
                }

                if (item.Type is "dir")
                {
                    await RecordLinksAndPathsAsync(fileLinks, item.Url);
                }
            }
        }

        private static string GetDirectoryPath(string filePath)
        {
            int lastSlashIndex = filePath.LastIndexOf('/');
            return lastSlashIndex == -1 ? string.Empty : filePath[..lastSlashIndex];
        }
    }

    public record FileLink(
        string Name, 
        string Path, 
        string DownloadUrl);
}
