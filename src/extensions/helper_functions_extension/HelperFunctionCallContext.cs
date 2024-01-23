using Azure.AI.OpenAI;

namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
{
    public class HelperFunctionCallContext
    {
        public HelperFunctionCallContext(HelperFunctionFactory factory)
        {
            _factory = factory;
        }

        public bool CheckForFunction(ChatResponseMessage message)
        {
            var updated = false;

            if (!string.IsNullOrEmpty(message.FunctionCall?.Name))
            {
                _functionName = message.FunctionCall.Name;
                updated = true;
            }

            if (!string.IsNullOrEmpty(message.FunctionCall?.Arguments))
            {
                _arguments = message.FunctionCall.Arguments;
                updated = true;
            }

            return updated;
        }

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

        public bool TryCallFunction(ChatCompletionsOptions options, out string? result)
        {
            return _factory.TryCallFunction(options, this, out result);
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
        private readonly HelperFunctionFactory _factory;
    }
}
