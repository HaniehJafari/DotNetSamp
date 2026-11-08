
class LargeObject
{
   
    public byte[] Data = new byte[1024 * 1024 * 10];
}


class MemorySafeManager : IDisposable
{
    
    private List<LargeObject> _objects = new List<LargeObject>();

    public Task AddObjects(int count, LargeObject obj)
    {
        for (int i = 0; i < count; i++)
        {
            _objects.Add(obj);
        }

        return Task.CompletedTask;
    }

    
    public void Dispose()
    {
       
        _objects.Clear();

        
    }
}

class Program
{
    static async Task Main(string[] args)
    {
       
        using var manager = new MemorySafeManager();

        var obj = new LargeObject();

        var tasks = Enumerable.Range(0, 20).Select(async i =>
        {
            await manager.AddObjects(5, obj);
            Console.WriteLine($"Iteration {i + 1}: Objects added.");
            await Task.Delay(500);
        });

        await Task.WhenAll(tasks);


        Console.WriteLine("Process finished and memory is safe. Press any key to exit...");
        Console.ReadKey();
    }
}