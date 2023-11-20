namespace Azure.AI.Details.Common.CLI.Extensions.FunctionCallingModel
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
