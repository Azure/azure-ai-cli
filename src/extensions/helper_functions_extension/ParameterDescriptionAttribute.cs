namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
{
    public class ParameterDescriptionAttribute : Attribute
    {
        public ParameterDescriptionAttribute()
        {
        }

        public ParameterDescriptionAttribute(string? description = null)
        {
            Description = description;
        }

        public string? Description { get; set; }
    }
}
