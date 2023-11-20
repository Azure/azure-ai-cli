using Azure.AI.OpenAI;

namespace Azure.AI.Details.Common.CLI.Extensions.FunctionCallingModel
{
    public static class Calculator
    {
        [FunctionDescription("Returns the age of a person")]
        public static int GetPersonAge([ParameterDescription("The name of the person whose age to return")] string personName)
        {
            return personName switch
            {
                "Beckett" => 17,
                "Jac" => 19,
                "Kris" => 53,
                "Rob" => 54,
                _ => 0,
            };
        }

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
