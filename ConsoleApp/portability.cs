
using System.Globalization;
using System.Text;

namespace PortableFileProcessor;

public sealed class FileStatistics
{
    public required string FileName { get; init; }
    public int LineCount { get; init; }
    public int WordCount { get; init; }
}

public sealed class FileProcessor
{
    public async Task<FileStatistics> ProcessAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        int lineCount = 0;
        int wordCount = 0;

        // UTF-8 with BOM detection.
        // Does not depend on the operating system's default encoding.
        using var reader = new StreamReader(
            filePath,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);

        while (await reader.ReadLineAsync(cancellationToken)
                   is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lineCount++;

            wordCount += line.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries).Length;
        }

        return new FileStatistics
        {
            FileName = Path.GetFileName(filePath),
            LineCount = lineCount,
            WordCount = wordCount
        };
    }
}

public sealed class ReportWriter
{
    public async Task WriteAsync(
        string reportPath,
        IEnumerable<FileStatistics> statistics,
        CancellationToken cancellationToken)
    {
        var report = new StringBuilder();

        report.AppendLine("PORTABLE FILE PROCESSING REPORT");
        report.AppendLine("--------------------------------");

        foreach (var item in statistics)
        {
            report.AppendLine($"File: {item.FileName}");
            report.AppendLine(
                $"Lines: {item.LineCount.ToString(CultureInfo.InvariantCulture)}");

            report.AppendLine(
                $"Words: {item.WordCount.ToString(CultureInfo.InvariantCulture)}");

            report.AppendLine();
        }

        // Write to a temporary file first.
        var tempPath = reportPath + ".tmp";

        try
        {
            await File.WriteAllTextAsync(
                tempPath,
                report.ToString(),
                new UTF8Encoding(false),
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // Replace the final report after successful writing.
            File.Move(tempPath, reportPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            // Resolve paths without OS-specific separators.
            string inputPath = Path.GetFullPath(
                args.Length > 0
                    ? args[0]
                    : Path.Combine(Environment.CurrentDirectory, "Input"));

            string outputPath = Path.GetFullPath(
                args.Length > 1
                    ? args[1]
                    : Path.Combine(Environment.CurrentDirectory, "Output"));

            if (!Directory.Exists(inputPath))
            {
                Console.Error.WriteLine(
                    $"Input directory not found: {inputPath}");

                return 1;
            }

            Directory.CreateDirectory(outputPath);

            string reportPath = Path.Combine(
                outputPath,
                "report.txt");

            var processor = new FileProcessor();
            var reportWriter = new ReportWriter();

            var results = new List<FileStatistics>();

            // Enumerate all files and filter extensions
            // without assuming case-sensitive or case-insensitive
            // filesystem behavior.
            var files = Directory.EnumerateFiles(
                    inputPath,
                    "*",
                    SearchOption.TopDirectoryOnly)
                .Where(file =>
                    string.Equals(
                        Path.GetExtension(file),
                        ".txt",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            Console.WriteLine($"Operating System: {Environment.OSVersion}");
            Console.WriteLine($"Input: {inputPath}");
            Console.WriteLine($"Output: {outputPath}");
            Console.WriteLine($"Found {files.Count} text file(s).");

            foreach (var file in files)
            {
                cts.Token.ThrowIfCancellationRequested();

                try
                {
                    var result = await processor.ProcessAsync(
                        file,
                        cts.Token);

                    results.Add(result);

                    Console.WriteLine(
                        $"Processed: {result.FileName} | " +
                        $"Lines: {result.LineCount} | " +
                        $"Words: {result.WordCount}");
                }
                catch (IOException ex)
                {
                    Console.Error.WriteLine(
                        $"Could not read {Path.GetFileName(file)}: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.Error.WriteLine(
                        $"Access denied: {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            await reportWriter.WriteAsync(
                reportPath,
                results,
                cts.Token);

            Console.WriteLine();
            Console.WriteLine("Processing completed successfully.");
            Console.WriteLine($"Report: {reportPath}");

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation was cancelled.");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            return 1;
        }
    }
}
