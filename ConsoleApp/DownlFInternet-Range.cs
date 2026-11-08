
using System.Net;
using System.Net.Http.Headers;

Console.WriteLine("=== Robust Console Downloader ===");

Console.Write("Enter file URL: ");
var url = Console.ReadLine();

Console.Write("Enter output file path: ");
var outputPath = Console.ReadLine();

if (string.IsNullOrWhiteSpace(url) ||
    string.IsNullOrWhiteSpace(outputPath) ||
    !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
    (uri.Scheme != Uri.UriSchemeHttp &&
     uri.Scheme != Uri.UriSchemeHttps))
{
    Console.WriteLine("Invalid URL or output path.");
    return;
}

using var cancellationSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellationSource.Cancel();
    Console.WriteLine("\nCancellation requested...");
};

using var httpClient = new HttpClient
{
    Timeout = Timeout.InfiniteTimeSpan
};

await DownloadFileAsync(
    httpClient,
    uri,
    outputPath,
    cancellationSource.Token
);

static async Task DownloadFileAsync(
    HttpClient client,
    Uri uri,
    string outputPath,
    CancellationToken cancellationToken)
{
    string partialPath = outputPath + ".download";

    const int maxRetries = 5;

    int retryCount = 0;

    Directory.CreateDirectory(
        Path.GetDirectoryName(
            Path.GetFullPath(outputPath))!
    );

    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            long downloadedBytes = File.Exists(partialPath)
                ? new FileInfo(partialPath).Length
                : 0;

            Console.WriteLine(
                $"\nDownloaded: {downloadedBytes:N0} bytes"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                uri
            );

            if (downloadedBytes > 0)
            {
                request.Headers.Range =
                    new RangeHeaderValue(downloadedBytes, null);
            }

            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );

            // If server supports resume, it should return 206.
            // If it returns 200, it ignores the Range header.
            bool isPartialResponse =
                response.StatusCode == HttpStatusCode.PartialContent;

            if (downloadedBytes > 0 && !isPartialResponse)
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine(
                        "Server does not support resume. Restarting download."
                    );

                    downloadedBytes = 0;
                }
                else
                {
                    response.EnsureSuccessStatusCode();
                }
            }
            else
            {
                response.EnsureSuccessStatusCode();
            }

            // Validate the returned Content-Range.
            if (isPartialResponse)
            {
                var range = response.Content.Headers.ContentRange;

                if (range?.From != downloadedBytes)
                {
                    throw new InvalidDataException(
                        "Invalid Content-Range returned by server."
                    );
                }
            }

            var fileMode = downloadedBytes == 0
                ? FileMode.Create
                : FileMode.Append;

            await using var fileStream = new FileStream(
                partialPath,
                fileMode,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true
            );

            await using var networkStream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken
                );

            byte[] buffer = new byte[81920];

            long totalDownloaded = downloadedBytes;

            long? totalSize = isPartialResponse
                ? response.Content.Headers.ContentRange?.Length
                : response.Content.Headers.ContentLength;

            int bytesRead;

            while ((bytesRead = await networkStream.ReadAsync(
                buffer.AsMemory(),
                cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(
                    buffer.AsMemory(0, bytesRead),
                    cancellationToken
                );

                totalDownloaded += bytesRead;

                if (totalSize.HasValue)
                {
                    Console.Write(
                        $"\rProgress: {totalDownloaded * 100.0 / totalSize.Value:F2}%"
                    );
                }
                else
                {
                    Console.Write(
                        $"\rDownloaded: {totalDownloaded:N0} bytes"
                    );
                }
            }

            await fileStream.FlushAsync(cancellationToken);

            // Verify the downloaded file size when known.
            if (totalSize.HasValue &&
                totalDownloaded != totalSize.Value)
            {
                throw new IOException(
                    "Downloaded file size does not match expected size."
                );
            }

            // Move completed partial file to final destination.
            File.Move(partialPath, outputPath, overwrite: true);

            Console.WriteLine("\n\nDownload completed successfully!");
            Console.WriteLine($"Saved to: {outputPath}");

            return;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine(
                "\nDownload cancelled. Partial file is preserved."
            );

            return;
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is IOException ||
            ex is TaskCanceledException)
        {
            retryCount++;

            if (retryCount > maxRetries)
            {
                Console.WriteLine(
                    $"\nDownload failed after {maxRetries} retries."
                );

                Console.WriteLine(ex.Message);

                Console.WriteLine(
                    $"Partial file preserved at: {partialPath}"
                );

                return;
            }

            int delaySeconds = Math.Min(
                (int)Math.Pow(2, retryCount),
                30
            );

            Console.WriteLine(
                $"\nDownload interrupted: {ex.Message}"
            );

            Console.WriteLine(
                $"Retry {retryCount}/{maxRetries} in {delaySeconds} seconds..."
            );

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(delaySeconds),
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(
                    "\nDownload cancelled. Partial file is preserved."
                );

                return;
            }
        }
    }
}
