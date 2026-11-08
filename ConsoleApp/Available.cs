using System;
using System.Threading.Tasks;

namespace DotnetAvailabilityConsoleApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Dotnet Availability Service...");

          
            while (true)
            {
                var service = new SampleService();
                try
                {
                 
                    await service.RunAsync();

                    break;
                }
                catch (Exception ex)
                {
                   
                    Console.WriteLine($"[Error] Service crashed: {ex.Message}");
                    Console.WriteLine("Restarting service in 2 seconds...");

                    await Task.Delay(2000);

                  
                }
            }
        }

    }

    public class SampleService
    {
        public async Task RunAsync()
        {
            Console.WriteLine("Service started.");
            await Task.Delay(1000);
            throw new Exception("Unexpected error in service.");
        }
    }
}