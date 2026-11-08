using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DIAnswer;

public interface IOperation
{
    string Execute();
}

public class OperationA : IOperation
{
    public string Execute() => "OperationA executed";
}

public class OperationB : IOperation
{
    public string Execute() => "OperationB executed";
}


public class Processor
{
    private readonly IServiceProvider _serviceProvider;

    public Processor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Process(string input)
    {
      
        var op = _serviceProvider.GetKeyedService<IOperation>(input.ToLower());

        if (op == null)
        {
            Console.WriteLine("Invalid input: Operation not found.");
            return;
        }

        Console.WriteLine(op.Execute());
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                
                services.AddKeyedTransient<IOperation, OperationA>("a");
                services.AddKeyedTransient<IOperation, OperationB>("b");

                services.AddTransient<Processor>();
            })
            .Build();

        var processor = host.Services.GetRequiredService<Processor>();

        Console.Write("Enter impl: a or b > ");
        string? userInput = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(userInput))
        {
            processor.Process(userInput);
        }
    }
}


////using var provider = services.BuildServiceProvider();
//// دریافت آخرین پیاده‌سازی ثبت‌شده
//var writer = provider.GetRequiredService<IMessageWriter>();

//// دریافت تمام پیاده‌سازی‌ها
//var writers = provider.GetServices<IMessageWriter>();

//// دریافت پیاده‌سازی مشخص با کلید
//var keyedWriter = provider.GetRequiredKeyedService<IMessageWriter>("console");