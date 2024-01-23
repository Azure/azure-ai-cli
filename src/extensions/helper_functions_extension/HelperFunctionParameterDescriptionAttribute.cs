namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
{
    public class HelperFunctionParameterDescriptionAttribute : Attribute
    {
        public HelperFunctionParameterDescriptionAttribute()
        {
        }

        public HelperFunctionParameterDescriptionAttribute(string? description = null)
        {
            Description = description;
        }

        public string? Description { get; set; }
    }
}
