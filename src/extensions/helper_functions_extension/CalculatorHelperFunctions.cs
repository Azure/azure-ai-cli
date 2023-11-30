using Azure.AI.OpenAI;

namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
{
    public static class CalculatorHelperFunctions
    {
        [HelperFunctionDescription("Adds two floats")]
        public static float AddFloats(float a, float b)
        {
            return a + b;
        }

        [HelperFunctionDescription("Subtracts two floats")]
        public static float SubtractFloats(float a, float b)
        {
            return a - b;
        }

        [HelperFunctionDescription("Multiplies two floats")]
        public static float MultiplyFloats(float a, float b)
        {
            return a * b;
        }

        [HelperFunctionDescription("Divides two floats")]
        public static float DivideFloats(float a, float b)
        {
            return a / b;
        }
        
        [HelperFunctionDescription("Adds two integers")]
        public static Int64 AddIntegers(Int64 a, Int64 b)
        {
            return a + b;
        }

        [HelperFunctionDescription("Subtracts two integers")]
        public static Int64 SubtractIntegers(Int64 a, Int64 b)
        {
            return a - b;
        }

        [HelperFunctionDescription("Multiplies two integers")]
        public static Int64 MultiplyIntegers(Int64 a, Int64 b)
        {
            return a * b;
        }

        [HelperFunctionDescription("Divides two integers")]
        public static Int64 DivideIntegers(Int64 a, Int64 b)
        {
            return a / b;
        }

        [HelperFunctionDescription("Calculates the average of a list of numbers")]
        public static double Average(IEnumerable<double> numbers)
        {
            var count = numbers.Count();
            return count == 0 ? 0 : numbers.Sum() / count;
        }

        [HelperFunctionDescription("Calculates the standard deviation of a list of numbers")]
        public static double StandardDeviation(double[] numbers)
        {
            var count = numbers.Count();
            if (count == 0) return 0;

            double average = Average(numbers);
            double sumOfSquaresOfDifferences = numbers.Select(val => (val - average) * (val - average)).Sum();
            return Math.Sqrt(sumOfSquaresOfDifferences / count);
        }
    }
}