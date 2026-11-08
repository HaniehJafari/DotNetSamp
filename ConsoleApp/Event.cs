namespace Example01;

public class EventPublisher
{
    public event EventHandler? ProcessCompleted;

    public async Task DoProcess()
    {
        await Task.Delay(100);
        OnProcessCompleted(EventArgs.Empty);
    }

    protected virtual void OnProcessCompleted(EventArgs e)
    {
        ProcessCompleted?.Invoke(this, e);
    }
}

public class EventSubscriber : IDisposable
{
    private readonly string _name;
    private readonly EventPublisher _publisher;

    public EventSubscriber(string name, EventPublisher publisher)
    {
        _name = name;
        _publisher = publisher;
        publisher.ProcessCompleted +=  HandleProcessCompleted!;
    }

    private void HandleProcessCompleted(object sender, EventArgs e)
    {
        Console.WriteLine($"{_name} received event.");
    }

    public void Dispose()
    {
        _publisher.ProcessCompleted -= HandleProcessCompleted!;
    }
}

class Event
{
    static async Task Main(string[] args)
    {
        var tasks = Enumerable.Range(1, 10000).Select(async i =>
        {
            var publisher = new EventPublisher();
            using var subscriber = new EventSubscriber($"Subscriber_{i}", publisher);
            await publisher.DoProcess();
        });
        
        await Task.WhenAll(tasks);
        
        Console.WriteLine("Completed. Press any key to exit.");
        Console.ReadKey();
    }
}