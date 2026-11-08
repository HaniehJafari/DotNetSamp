
//using Microsoft.Extensions.DependencyInjection;

// 1. تعریف قرارداد مشترک Strategy
using Microsoft.Extensions.DependencyInjection;

public interface IOperationStrategy
{
    double Execute(int a, int b);
}

// 2. پیاده‌سازی عملیات جمع
public class AddStrategy : IOperationStrategy
{
    public double Execute(int a, int b) => a + b;
}

// 3. پیاده‌سازی عملیات تفریق
public class SubtractStrategy : IOperationStrategy
{
    public double Execute(int a, int b) => a - b;
}

// 4. پیاده‌سازی عملیات ضرب
public class MultiplyStrategy : IOperationStrategy
{
    public double Execute(int a, int b) => a * b;
}

// 5. پیاده‌سازی عملیات تقسیم
public class DivideStrategy : IOperationStrategy
{
    public double Execute(int a, int b)
    {
        if (b == 0)
            throw new DivideByZeroException(
                "Cannot divide by zero.");

        return (double)a / b;
    }
}

// 6. کلاس انتخاب و اجرای Strategy
public class Calculator
{
    private readonly IEnumerable<IOperationStrategy> _strategies;

    public Calculator(IEnumerable<IOperationStrategy> strategies)
    {
        _strategies = strategies;
    }

    public double Calculate(string operation, int a, int b)
    {
        IOperationStrategy? strategy = operation.ToLowerInvariant() switch
        {
            "add" => _strategies.OfType<AddStrategy>().FirstOrDefault(),
            "subtract" => _strategies.OfType<SubtractStrategy>().FirstOrDefault(),
            "multiply" => _strategies.OfType<MultiplyStrategy>().FirstOrDefault(),
            "divide" => _strategies.OfType<DivideStrategy>().FirstOrDefault(),
            _ => null
        };

        if (strategy is null)
            throw new ArgumentException("Invalid operation.");

        return strategy.Execute(a, b);
    }
}

public class Program
{
    public static void Main()
    {
        // 7. ثبت سرویس‌ها در DI Container
        var services = new ServiceCollection();

        services.AddSingleton<IOperationStrategy, AddStrategy>();
        services.AddSingleton<IOperationStrategy, SubtractStrategy>();
        services.AddSingleton<IOperationStrategy, MultiplyStrategy>();
        services.AddSingleton<IOperationStrategy, DivideStrategy>();

        services.AddTransient<Calculator>();

        using var serviceProvider = services.BuildServiceProvider();

        // 8. دریافت Calculator از DI Container
        var calculator = serviceProvider.GetRequiredService<Calculator>();

        Console.Write("Enter integer a: ");
        if (!int.TryParse(Console.ReadLine(), out int a))
        {
            Console.WriteLine("Invalid input for a.");
            return;
        }

        Console.Write("Enter integer b: ");
        if (!int.TryParse(Console.ReadLine(), out int b))
        {
            Console.WriteLine("Invalid input for b.");
            return;
        }

        Console.Write("Enter operation (Add, Subtract, Multiply, Divide): ");
        string operation = Console.ReadLine()?.Trim() ?? "";

        try
        {
            double result = calculator.Calculate(operation, a, b);
            Console.WriteLine($"Result: {result}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch (DivideByZeroException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
