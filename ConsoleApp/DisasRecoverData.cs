
using System;
using System.IO;
using System.Threading.Tasks;

namespace DotnetDisasterRecoveryConsole
{
    class Program
    {
        private static readonly string DataFilePath = "data.txt";
        private static readonly string BackupFilePath = "backup.txt";
        private static readonly string TempBackupFilePath = "backup.tmp";

        static async Task Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("\n1. Write Data");
                Console.WriteLine("2. Simulate Disaster (Delete Data)");
                Console.WriteLine("3. Recover Data");
                Console.WriteLine("4. Show Data");
                Console.WriteLine("5. Exit");

                Console.Write("Select option: ");

                var input = Console.ReadLine();

                try
                {
                    switch (input)
                    {
                        case "1":
                            await WriteData();
                            break;

                        case "2":
                            SimulateDisaster();
                            break;

                        case "3":
                            RecoverData();
                            break;

                        case "4":
                            ShowData();
                            break;

                        case "5":
                            return;

                        default:
                            Console.WriteLine("Invalid option.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        // Write data and create atomic backup
        static async Task WriteData()
        {
            Console.Write("Enter data: ");
            var data = Console.ReadLine();

            if (data == null)
                return;

            // Step 1: Save data to the main file
            await File.WriteAllTextAsync(DataFilePath, data);

            // Step 2: Create atomic backup
            CreateAtomicBackup();

            Console.WriteLine("Data saved and backup created successfully.");
        }

        // Atomic backup implementation
        static void CreateAtomicBackup()
        {
            try
            {
                // Step 1: Write data to temporary backup file
                File.Copy(DataFilePath, TempBackupFilePath, true);

                // Step 2: Replace or create backup file
                if (File.Exists(BackupFilePath))
                {
                    File.Replace(
                        TempBackupFilePath,
                        BackupFilePath,
                        null
                    );
                }
                else
                {
                    File.Move(
                        TempBackupFilePath,
                        BackupFilePath
                    );
                }
            }
            finally
            {
                // Clean up temporary file if it still exists
                if (File.Exists(TempBackupFilePath))
                {
                    File.Delete(TempBackupFilePath);
                }
            }
        }

        // Simulate disaster
        static void SimulateDisaster()
        {
            if (File.Exists(DataFilePath))
            {
                File.Delete(DataFilePath);
                Console.WriteLine("Disaster simulated. Data file deleted.");
            }
            else
            {
                Console.WriteLine("Data file does not exist.");
            }
        }

        // Recover data from backup
        static void RecoverData()
        {
            if (!File.Exists(BackupFilePath))
            {
                Console.WriteLine("No backup available.");
                return;
            }

            if (File.Exists(DataFilePath))
            {
                Console.WriteLine("Data file already exists.");
                return;
            }

            File.Copy(BackupFilePath, DataFilePath);

            Console.WriteLine("Data recovered successfully.");
        }

        // Show data
        static void ShowData()
        {
            if (File.Exists(DataFilePath))
            {
                var data = File.ReadAllText(DataFilePath);
                Console.WriteLine($"Data: {data}");
            }
            else
            {
                Console.WriteLine("No data file found.");
            }
        }
    }
}
