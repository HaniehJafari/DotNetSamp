namespace OptimizedCounterApp
{
    class Counter
    {
        private int _value = 0;
        //public int Value => _value;
        public int Value=> Volatile.Read(ref _value);

        public void Increment()
        {
          
            Interlocked.Increment(ref _value);
        }
    }

    class DataProcessor
    {
        public async Task ProcessAsync(Counter counter, int iterations)
        {
            List<Task> tasks = new List<Task>(iterations);
            
            for (int i = 0; i < iterations; i++)
            {
                tasks.Add(Task.Run(() => counter.Increment()));
            }
            await Task.WhenAll(tasks);
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var counter = new Counter();
            var processor = new DataProcessor();
            Console.WriteLine("Processing tasks concurrently...");
            await processor.ProcessAsync(counter, 1000);
            Console.WriteLine($"Final Counter Value: {counter.Value}");
        }
    }
}