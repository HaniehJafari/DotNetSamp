
class Program
{
    static async Task Main(string[] args)
    {
        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "SampleFiles");
        
       
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            for (int i = 1; i <= 5; i++)
            {
                await File.WriteAllTextAsync(Path.Combine(folderPath, $"file{i}.txt"), $"Content of file {i}");
            }
        }

       
        var files = Directory.GetFiles(folderPath, "*.txt");

       
        var readTasks = files.Select(file => File.ReadAllTextAsync(file));

       
        string[] fileContents = await Task.WhenAll(readTasks);

      
        string aggregatedContent = string.Join(Environment.NewLine, fileContents);

        Console.WriteLine("Aggregated content:");
        Console.WriteLine(aggregatedContent);
    }
}