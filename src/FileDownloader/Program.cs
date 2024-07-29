using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileDownloader
{
    public class Program
    {
        private async static Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Specify the input file path and output directory.");
                return;
            }

            var inputFilePath = args[0];
            var outputDirectory = args[1];

            if (!File.Exists(inputFilePath))
            {
                Console.WriteLine($"File {inputFilePath} does not exist.");
                return;
            }

            CreateDirectories(outputDirectory);

            using var fileStream = new FileStream(
                path: inputFilePath, mode: FileMode.Open, access: FileAccess.Read);

            var fileLinks = await JsonSerializer
                .DeserializeAsync<List<FileLink>>(fileStream);

            int totalFiles = fileLinks.Count;
            int downloadedFiles = 0;

            var failDownloads = new List<string>();

            using (var httpClient = new HttpClient(new HttpClientHandler()
            {
                Proxy = new WebProxy
                {
                    Address = new Uri(""),
                    UseDefaultCredentials = true,
                }
            }
            ))
            {
                foreach (var fileLink in fileLinks)
                {
                    try
                    {
                        var path = $"{outputDirectory}/{fileLink.Path}";
                        var uri = new Uri(fileLink.DownloadUrl);

                        CreateDirectories(path);

                        byte[] fileBytes = await httpClient.GetByteArrayAsync(uri);
                        await File.WriteAllBytesAsync($"{path}/{fileLink.Name}", fileBytes);
                        downloadedFiles++;
                    }
                    catch (Exception)
                    {
                        failDownloads.Add(fileLink.DownloadUrl);
                    }

                    DisplayProgressBar(downloadedFiles, totalFiles);
                }
            }

            if (failDownloads.Count > 0)
            {
                foreach (var file in failDownloads)
                {
                    Console.WriteLine(file);
                }
            }

            Console.ReadKey();
        }

        private static void CreateDirectories(string path)
        {
            if (!Directory.Exists(path) && path is not "")
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void DisplayProgressBar(int complete, int total)
        {
            const int barWidth = 24;
            var progress = (float)complete / total;
            var filledBars = (int)(progress * barWidth);

            Console.CursorLeft = 0;
            Console.Write("[");
            Console.Write(new string('#', filledBars));
            Console.Write(new string('.', barWidth - filledBars));
            Console.Write($"] {complete}/{total} files");
        }
    }

    public record FileLink(
        string Name,
        string Path, 
        string DownloadUrl);
}
