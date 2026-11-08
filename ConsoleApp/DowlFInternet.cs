using System.Net;
using System.Net.Http.Headers;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Enter file URL:");
        var url = Console.ReadLine();

        Console.WriteLine("Enter destination file name (e.g., video.mp4):");
        var dest = Console.ReadLine();

        var service = new DownloadService();
        try
        {
            Console.WriteLine("Starting download...");
            await service.DownloadFileWithResumeAsync(url, dest);
            Console.WriteLine("\nDownload completed successfully!");
        }
        catch (HttpRequestException ex)
        {
           
            Console.WriteLine($"\n[Network Error]: Internet connection lost or server unreachable. Details: {ex.Message}");
            Console.WriteLine("You can run the application again to resume the download.");
        }
        catch (IOException ex)
        {
            
            Console.WriteLine($"\n[IO Error]: File system error (e.g., disk full or file in use). Details: {ex.Message}");
        }
        catch (Exception ex)
        {
            
            Console.WriteLine($"\n[Unexpected Error]: {ex.Message}");
        }
    }
}

public class DownloadService
{
    
    private static readonly HttpClient _httpClient = new HttpClient();
    private const int BufferSize = 81920; // 80 KB

    public async Task DownloadFileWithResumeAsync(string url, string destination)
    {
       
        string partFilePath = $"{destination}.part";
        long existingLength = 0;

        
        if (File.Exists(partFilePath))
        {
            existingLength = new FileInfo(partFilePath).Length;
            Console.WriteLine($"Found incomplete download. Resuming from {existingLength} bytes...");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        
        if (existingLength > 0)
        {
            request.Headers.Range = new RangeHeaderValue(existingLength, null);
        }

        
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        
        if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            Console.WriteLine("File is already fully downloaded or range is invalid.");
            return;
        }

        response.EnsureSuccessStatusCode();

       
        if (existingLength > 0 && response.StatusCode == HttpStatusCode.OK)
        {
            Console.WriteLine("Server does not support resuming. Restarting download from scratch...");
            existingLength = 0; 
        }

        using var inputStream = await response.Content.ReadAsStreamAsync();

       
        using var outputStream = new FileStream(
            partFilePath,
            existingLength == 0 ? FileMode.Create : FileMode.Append,
            FileAccess.Write,
            FileShare.None,
            BufferSize,
            FileOptions.Asynchronous);

        var buffer = new byte[BufferSize];
        int bytesRead;

       
        while ((bytesRead = await inputStream.ReadAsync(buffer.AsMemory(0, BufferSize))) > 0)
        {
            await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead));
           
            Console.Write(".");
        }

       
        outputStream.Close();

        if (File.Exists(destination))
        {
            File.Delete(destination); 
        }

        File.Move(partFilePath, destination);
    }
}