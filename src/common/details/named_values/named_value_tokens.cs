namespace Azure.AI.Details.Common.CLI
{
    public abstract class INamedValueTokens
    {
        public abstract string PopNextToken();
        public abstract string PopNextTokenValue(INamedValues values = null);

        public abstract string PeekNextToken(int skip = 0);
        public abstract string PeekNextTokenValue(int skip = 0, INamedValues values = null);

        public abstract string PeekAllTokens(int count = int.MaxValue);

        public abstract void SkipTokens(int count);

        public abstract string ValueFromToken(string token, INamedValues values = null);

        public abstract string NamePrefixRequired();
    }
}
