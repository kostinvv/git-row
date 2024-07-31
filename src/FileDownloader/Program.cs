using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FileDownloader
{
    internal class Program
    {
        private const string ProxyUrl = "";

        private const int MaxConcurrentRequests = 4;

        private static SocketsHttpHandler socketsHttpHandler = new()
        {
            MaxConnectionsPerServer = MaxConcurrentRequests,
            /*Proxy = new WebProxy()
            {
                Address = new Uri(ProxyUrl),
                UseDefaultCredentials = true,
            },*/
        };

        private static SemaphoreSlim semaphore = new(initialCount: MaxConcurrentRequests);

        private static HttpClient _httpClient = new(socketsHttpHandler);

        private record JSONFileLink(string Name, string Path, string DownloadUrl);

        private async static Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Specify the [JSON File Path] and [Output Directory].");
                return;
            }

            string jsonFilePath = args[0];

            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine($"File {jsonFilePath} does not exist.");
                return;
            }

            string downloadDirectory = args[1];

            if (!Directory.Exists(downloadDirectory))
            {
                Directory.CreateDirectory(downloadDirectory);
            }

            var fileLinks = new List<JSONFileLink>(); 
            var tasks = new List<Task>();
            var watch = new Stopwatch();
            var failDownloads = new List<string>();

            using (var fileStream = new FileStream(path: jsonFilePath, mode: FileMode.Open, access: FileAccess.Read))
            {
                fileLinks = await JsonSerializer.DeserializeAsync<List<JSONFileLink>>(fileStream);
            };

            int downloadFiles = 0;

            watch.Start();
            foreach (var fileLink in fileLinks)
            {
                tasks.Add(Task.Run(async () =>
                {
                    string fullPath = $"{downloadDirectory}/{fileLink.Path}";

                    if (!Directory.Exists(fullPath) && fullPath is not "")
                    {
                        Directory.CreateDirectory(fullPath);
                    }

                    byte[] fileBytes = await DownloadFileAsync(new Uri(fileLink.DownloadUrl));

                    if (fileBytes is null)
                    {
                        lock (failDownloads)
                        {
                            failDownloads.Add(fileLink.Name);
                        }
                        return;
                    }

                    await File.WriteAllBytesAsync($"{fullPath}/{fileLink.Name}", fileBytes);

                    Interlocked.Increment(ref downloadFiles);
                    Console.Write($"\r{downloadFiles}/{fileLinks.Count} files");
                }));
            }
            await Task.WhenAll(tasks);
            watch.Stop();

            TimeSpan timeTaken = watch.Elapsed;
            var timeFormatted = string.Format("{0:D2}m:{1:D2}s:{2:D3}ms", 
                timeTaken.Minutes, 
                timeTaken.Seconds, 
                timeTaken.Milliseconds);

            Console.WriteLine($"\nTime: {timeFormatted}");

            if (failDownloads.Count > 0)
            {
                Console.WriteLine("\nFail: ");
                for (var i = 1; i < failDownloads.Count; i++)
                {
                    Console.WriteLine($"{i}. {failDownloads[i - 1]}");
                }
            }

            Console.ReadKey();
        }

        private async static Task<byte[]> DownloadFileAsync(Uri uri)
        {
            try
            {
                await semaphore.WaitAsync();

                return await _httpClient.GetByteArrayAsync(uri);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
