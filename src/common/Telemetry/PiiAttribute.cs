namespace Azure.AI.Details.Common.CLI.Telemetry
{
    public enum PiiKind
    {
        None,
        UserId,
        IP4Address,
        IP6Address,
        Uri
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PiiAttribute : Attribute
    {
        public PiiAttribute(PiiKind kind)
        {
            Kind = kind;
        }

        public PiiKind Kind { get; set; }
    }
}
