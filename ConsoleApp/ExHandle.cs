namespace Dotnet9ConsoleExceptionHandling
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Enter numbers separated by comma (e.g., 2,4,5):");
            string? input = Console.ReadLine();

          
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Input cannot be empty.");
                return;
            }

            string[] tokens = input.Split(',');
            List<int> numbers = new List<int>();

            foreach (var token in tokens)
            {
               
                if (int.TryParse(token.Trim(), out int number))
                {
                    
                    if (number == 0)
                    {
                        Console.WriteLine("Warning: Zero is not allowed (division by zero). Skipping '0'.");
                        continue; 
                    }
                    
                    numbers.Add(number);
                }
                else
                {
                   
                    Console.WriteLine($"Warning: Invalid input '{token}'. Skipping non-numeric value.");
                }
            }

           
            if (numbers.Count == 0)
            {
                Console.WriteLine("No valid numbers were provided for calculation.");
                return;
            }

           
            double result = CalculateSumOfReciprocals(numbers);
            
           
            Console.WriteLine($"Sum of reciprocals: {result:F2}");
        }

       
        public static double CalculateSumOfReciprocals(List<int> numbers)
        {
            double sum = 0;
            foreach (var n in numbers)
            {
                
                sum += 1.0 / n; 
            }
            return sum;
        }
    }
}
