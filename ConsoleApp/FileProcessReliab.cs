
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileProcessingReliabilityDotnet10
{
    public class Program
    {
        private const int MaxDegreeOfParallelism = 4;

        public static async Task Main(string[] args)
        {
            string inputDirectory = "input";
            string outputDirectory = "output";

            Directory.CreateDirectory(inputDirectory);
            Directory.CreateDirectory(outputDirectory);

            string[] files = Directory.GetFiles(
                inputDirectory,
                "*.txt");

            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();

                Console.WriteLine(
                    "\nCancellation requested...");
            };

            var errors = new ConcurrentBag<string>();

            int processedCount = 0;
            int skippedCount = 0;

            Console.WriteLine($"Found {files.Length} files.");

            try
            {
                await Parallel.ForEachAsync(
                    files,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism =
                            MaxDegreeOfParallelism,

                        CancellationToken = cts.Token
                    },
                    async (inputFile, token) =>
                    {
                        try
                        {
                            string outputFile = Path.Combine(
                                outputDirectory,
                                Path.GetFileName(inputFile));

                            bool processed = await ProcessFileAsync(
                                inputFile,
                                outputFile,
                                token);

                            if (processed)
                            {
                                Interlocked.Increment(
                                    ref processedCount);

                                Console.WriteLine(
                                    $"Processed: {Path.GetFileName(inputFile)}");
                            }
                            else
                            {
                                Interlocked.Increment(
                                    ref skippedCount);

                                Console.WriteLine(
                                    $"Skipped: {Path.GetFileName(inputFile)}");
                            }
                        }
                        catch (OperationCanceledException)
                            when (token.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            errors.Add(
                                $"{Path.GetFileName(inputFile)}: {ex.Message}");

                            Console.WriteLine(
                                $"Failed: {Path.GetFileName(inputFile)}");
                        }
                    });
            }
            catch (OperationCanceledException)
                when (cts.IsCancellationRequested)
            {
                Console.WriteLine("Processing was cancelled.");
            }

            Console.WriteLine("\n========== Summary ==========");
            Console.WriteLine($"Total files: {files.Length}");
            Console.WriteLine($"Processed: {processedCount}");
            Console.WriteLine($"Skipped: {skippedCount}");
            Console.WriteLine($"Failed: {errors.Count}");

            foreach (var error in errors)
            {
                Console.WriteLine(error);
            }

            Console.WriteLine("=============================");
        }

        private static async Task<bool> ProcessFileAsync(
            string inputFile,
            string outputFile,
            CancellationToken cancellationToken)
        {
            string metadataFile = outputFile + ".sha256";

            // Read input once so the hash and processed
            // content refer to the same snapshot.
            byte[] inputBytes = await File.ReadAllBytesAsync(
                inputFile,
                cancellationToken);

            string inputHash = Convert.ToHexString(
                SHA256.HashData(inputBytes));

            // Check whether this input has already been processed.
            if (await IsAlreadyProcessedAsync(
                outputFile,
                metadataFile,
                inputHash,
                cancellationToken))
            {
                return false;
            }

            string tempOutput = outputFile + "." +
                                Guid.NewGuid().ToString("N") + ".tmp";

            string tempMetadata = metadataFile + "." +
                                  Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                // Read lines from the same byte snapshot.
                string[] lines = ReadLines(inputBytes);

                // Process data.
                string[] processedLines = ProcessLines(lines);

                cancellationToken.ThrowIfCancellationRequested();

                // Write to temporary output first.
                await File.WriteAllLinesAsync(
                    tempOutput,
                    processedLines,
                    cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                // Publish the complete output.
                // Both files are in the same directory.
                File.Move(
                    tempOutput,
                    outputFile,
                    overwrite: true);

                // Write metadata only after output publication.
                await File.WriteAllTextAsync(
                    tempMetadata,
                    inputHash,
                    cancellationToken);

                File.Move(
                    tempMetadata,
                    metadataFile,
                    overwrite: true);

                return true;
            }
            finally
            {
                // Remove temporary files after success or failure.
                TryDelete(tempOutput);
                TryDelete(tempMetadata);
            }
        }

        private static async Task<bool> IsAlreadyProcessedAsync(
            string outputFile,
            string metadataFile,
            string inputHash,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(outputFile) ||
                !File.Exists(metadataFile))
            {
                return false;
            }

            string savedHash = (
                await File.ReadAllTextAsync(
                    metadataFile,
                    cancellationToken)).Trim();

            return string.Equals(
                savedHash,
                inputHash,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string[] ReadLines(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);

            using var reader = new StreamReader(
                stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true);

            var lines = new System.Collections.Generic.List<string>();

            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }

            return lines.ToArray();
        }

        private static string[] ProcessLines(string[] lines)
        {
            var result = new string[lines.Length];

            for (int i = 0; i < lines.Length; i++)
            {
                result[i] = lines[i].ToUpperInvariant();
            }

            return result;
        }

        private static void TryDelete(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (IOException)
            {
                // Temporary file cleanup can be retried
                // during the next application execution.
            }
            catch (UnauthorizedAccessException)
            {
                // Log this in a production application.
            }
        }
    }
}
