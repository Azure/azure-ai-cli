using Azure.AI.OpenAI;
using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;

namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions.Test
{
    public static class TestExtension
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
    }
}
