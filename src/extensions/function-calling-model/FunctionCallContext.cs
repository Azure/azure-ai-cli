using Azure.AI.OpenAI;

namespace Azure.AI.Details.Common.CLI.Extensions.FunctionCallingModel
{
    public class FunctionCallContext
    {
        public bool CheckForUpdate(StreamingChatCompletionsUpdate update)
        {
            var updated = false;

            if (!string.IsNullOrEmpty(update.FunctionName))
            {
                _functionName = update.FunctionName;
                updated = true;
            }
            
            var arguments = update.FunctionArgumentsUpdate;
            if (arguments != null)
            {
                _arguments += arguments;
                updated = true;
            }

            return updated;
        }

        public void Reset()
        {
            _functionName = string.Empty;
            _arguments = string.Empty;
        }

        public string FunctionName => _functionName;

        public string Arguments => _arguments;

        private string _functionName = string.Empty;
        private string _arguments = string.Empty;
    }
}
