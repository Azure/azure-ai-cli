namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
{
    public class FunctionDescriptionAttribute : Attribute
    {
        public FunctionDescriptionAttribute()
        {
        }

        public FunctionDescriptionAttribute(string description)
        {
            Description = description;
        }

        public string? Description { get; set; }
    }
}
