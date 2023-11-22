using Azure.AI.OpenAI;

namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
{
    public static class Calculator
    {
        [FunctionDescription("Adds two numbers")]
        public static int Add(int a, int b)
        {
            return a + b;
        }

        [FunctionDescription("Subtracts two numbers")]
        public static int Subtract(int a, int b)
        {
            return a - b;
        }

        [FunctionDescription("Multiplies two numbers")]
        public static int Multiply(int a, int b)
        {
            return a * b;
        }

        [FunctionDescription("Divides two numbers")]
        public static int Divide(int a, int b)
        {
            return a / b;
        }
    }
}
