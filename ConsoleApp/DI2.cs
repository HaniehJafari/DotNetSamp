
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dotnet9ConsoleDiChallenge;

public interface IOperation
{
    string Execute();
}

public class OperationA : IOperation
{
    public string Execute()
    {
        return "OperationA executed";
    }
}

public class OperationB : IOperation
{
    public string Execute()
    {
        return "OperationB executed";
    }
}

public class OperationResolver
{
    private readonly IServiceProvider _provider;

    public OperationResolver(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IOperation Resolve(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be empty.");

        var key = input.Trim().ToLowerInvariant();

        return _provider.GetRequiredKeyedService<IOperation>(key);
    }
}

public class Processor
{
    private readonly OperationResolver _resolver;

    public Processor(OperationResolver resolver)
    {
        _resolver = resolver;
    }

    public void Process(string input)
    {
        var operation = _resolver.Resolve(input);

        Console.WriteLine(operation.Execute());
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                // Register keyed implementations
                services.AddKeyedTransient<IOperation, OperationA>("a");
                services.AddKeyedTransient<IOperation, OperationB>("b");

                // Register application services
                services.AddTransient<OperationResolver>();
                services.AddTransient<Processor>();
            })
            .Build();

        using (host)
        {
            var processor = host.Services.GetRequiredService<Processor>();

            Console.Write("Enter impl: a or b > ");

            string? userInput = Console.ReadLine();

            try
            {
                processor.Process(userInput ?? string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
