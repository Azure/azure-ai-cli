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
