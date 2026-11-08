////1- Estastone
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;

//namespace DotnetPerformanceTuningConsoleApp
//{
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            var processor = new DataProcessor();

//            var data = processor.GenerateData(1_000_000);

//            var stopwatch = Stopwatch.StartNew();

//            var result = processor.ProcessData(data);

//            stopwatch.Stop();

//            Console.WriteLine(
//                $"Processed {result.Count} prime numbers " +
//                $"in {stopwatch.ElapsedMilliseconds} ms");

//            Console.WriteLine("First 10 prime numbers:");

//            foreach (var number in result.Take(10))
//            {
//                Console.Write($"{number} ");
//            }
//        }
//    }

//    public class DataProcessor
//    {
//        public List<int> GenerateData(int count)
//        {
//            if (count < 0)
//                throw new ArgumentOutOfRangeException(nameof(count));

//            var data = new List<int>(count);

//            for (int i = 0; i < count; i++)
//            {
//                data.Add(i);
//            }

//            return data;
//        }

//        public List<int> ProcessData(List<int> data)
//        {
//            ArgumentNullException.ThrowIfNull(data);

//            if (data.Count == 0)
//                return new List<int>();

//            int max = data.Max();

//            if (max < 2)
//                return new List<int>();

//            // Create sieve
//            var isPrime = new bool[max + 1];

//            Array.Fill(isPrime, true);

//            isPrime[0] = false;
//            isPrime[1] = false;

//            // Sieve algorithm
//            for (int i = 2; i <= max / i; i++)
//            {
//                if (!isPrime[i])
//                    continue;

//                for (long j = (long)i * i; j <= max; j += i)
//                {
//                    isPrime[(int)j] = false;
//                }
//            }

//            // Collect results
//            var result = new List<int>();

//            foreach (var item in data)
//            {
//                if (item >= 2 && isPrime[item])
//                {
//                    result.Add(item);
//                }
//            }

//            return result;
//        }
//    }
//}









////2-another

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DotnetPerformanceTuningConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var processor = new DataProcessor();

            // Generate one million numbers
            var data = processor.GenerateData(1_000_000);

            var stopwatch = Stopwatch.StartNew();

            var result = processor.ProcessData(data);

            stopwatch.Stop();

            Console.WriteLine(
                $"Processed {result.Count} prime numbers " +
                $"in {stopwatch.ElapsedMilliseconds} ms");

            Console.WriteLine("First 10 prime numbers:");

            foreach (var number in result.GetRange(
                         0, Math.Min(10, result.Count)))
            {
                Console.Write($"{number} ");
            }
        }
    }

    public class DataProcessor
    {
        public List<int> GenerateData(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            // Optimization 1: Set initial List capacity
            var data = new List<int>(count);

            for (int i = 0; i < count; i++)
            {
                data.Add(i);
            }

            return data;
        }

        public List<int> ProcessData(List<int> data)
        {
            ArgumentNullException.ThrowIfNull(data);

            // Set initial capacity for result
            var result = new List<int>();

            foreach (var item in data)
            {
                if (IsPrime(item))
                {
                    result.Add(item);
                }
            }

            return result;
        }

        // Optimization 2: Optimized Prime Number Algorithm
        private bool IsPrime(int number)
        {
            if (number < 2)
                return false;

            if (number == 2)
                return true;

            // Remove even numbers
            if (number % 2 == 0)
                return false;

            // Check only odd divisors up to square root
            for (int i = 3; i * i <= number; i += 2)
            {
                if (number % i == 0)
                    return false;
            }

            return true;
        }
    }
}
