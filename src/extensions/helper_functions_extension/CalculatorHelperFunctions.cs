using Azure.AI.OpenAI;

namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
{
    public static class CalculatorHelperFunctions
    {
        [HelperFunctionDescription("Adds two numbers")]
        public static int Add(int a, int b)
        {
            return a + b;
        }

        [HelperFunctionDescription("Subtracts two numbers")]
        public static int Subtract(int a, int b)
        {
            return a - b;
        }

        [HelperFunctionDescription("Multiplies two numbers")]
        public static int Multiply(int a, int b)
        {
            return a * b;
        }

        [HelperFunctionDescription("Divides two numbers")]
        public static int Divide(int a, int b)
        {
            return a / b;
        }
    }
}
