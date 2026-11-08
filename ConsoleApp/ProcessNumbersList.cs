namespace Example02Answer;

class Program
{
    static async Task Main(string[] args)
    {
        var numbers = Enumerable.Range(1, 100);

        
        var tasks = numbers.Select(async number =>
        {
            await Task.Delay(10);

            
            return number * 2;
        });

   
        var results = await Task.WhenAll(tasks);

        Console.WriteLine($"Sum: {results.Sum()}");
    }
}



// second way
class Program2
{
    static List<int> results = new List<int>();
 
    private static readonly object _lockObject = new object();

    static async Task Main(string[] args)
    {
        var numbers = Enumerable.Range(1, 100).ToList();
        var tasks = new List<Task>();

        foreach (var number in numbers)
        {
            tasks.Add(ProcessNumberAsync(number));
        }

        await Task.WhenAll(tasks);
        Console.WriteLine($"Sum: {results.Sum()}");
    }

    static async Task ProcessNumberAsync(int number)
    {
       
        await Task.Delay(10);

        int calculatedValue = number * 2;

      
        lock (_lockObject)
        {
            results.Add(calculatedValue);
        }
    }
}