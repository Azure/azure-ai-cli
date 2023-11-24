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
    }
}