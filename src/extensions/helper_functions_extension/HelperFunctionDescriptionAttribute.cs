namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
{
    public class HelperFunctionDescriptionAttribute : Attribute
    {
        public HelperFunctionDescriptionAttribute()
        {
        }

        public HelperFunctionDescriptionAttribute(string description)
        {
            Description = description;
        }

        public string? Description { get; set; }
    }
}
