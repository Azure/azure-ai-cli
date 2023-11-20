namespace Azure.AI.Details.Common.CLI.Extensions.FunctionCallingModel
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
