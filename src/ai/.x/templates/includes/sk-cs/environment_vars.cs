        var AZURE_OPENAI_SYSTEM_PROMPT = Environment.GetEnvironmentVariable("AZURE_OPENAI_SYSTEM_PROMPT") ?? "You are a helpful AI assistant.";

        // NOTE: Never deploy your API Key in client-side environments like browsers or mobile apps
        // SEE: https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety

        // Get the required environment variables
        var AZURE_OPENAI_API_KEY = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "<insert your Azure OpenAI API key here>";
        {{if {_IS_WITH_DATA_TEMPLATE}}}
        var AZURE_OPENAI_API_VERSION = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_VERSION") ?? "<insert your Azure OpenAI API version here>";
        {{endif}}
        var AZURE_OPENAI_ENDPOINT = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "<insert your Azure OpenAI endpoint here>";
        var AZURE_OPENAI_CHAT_DEPLOYMENT = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT") ?? "<insert your Azure OpenAI chat deployment name here>";
        {{if {_IS_WITH_DATA_TEMPLATE}}}
        var AZURE_AI_SEARCH_KEY = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_KEY") ?? "<insert your Azure AI Search key here>";
        var AZURE_AI_SEARCH_ENDPOINT = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_ENDPOINT") ?? "<insert your Azure AI Search endpoint here>";
        var AZURE_AI_SEARCH_INDEX_NAME = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_INDEX_NAME") ?? "<insert your Azure AI Search index name here>";
        var AZURE_OPENAI_EMBEDDING_DEPLOYMENT = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? "<insert your OpenAI embeddings deployment name here>";
        {{endif}}

        // Check if the required environment variables are set
        var azureOk = 
            AZURE_OPENAI_API_KEY != null && !AZURE_OPENAI_API_KEY.StartsWith("<insert") &&
            {{if {_IS_WITH_DATA_TEMPLATE}}}
            AZURE_OPENAI_API_VERSION != null && !AZURE_OPENAI_API_VERSION.StartsWith("<insert") &&
            {{endif}}
            AZURE_OPENAI_CHAT_DEPLOYMENT != null && !AZURE_OPENAI_CHAT_DEPLOYMENT.StartsWith("<insert") &&
            AZURE_OPENAI_ENDPOINT != null && !AZURE_OPENAI_ENDPOINT.StartsWith("<insert");
        {{if {_IS_WITH_DATA_TEMPLATE}}}
        var searchOk = 
            AZURE_AI_SEARCH_KEY != null && !AZURE_AI_SEARCH_KEY.StartsWith("<insert") &&
            AZURE_AI_SEARCH_ENDPOINT != null && !AZURE_AI_SEARCH_ENDPOINT.StartsWith("<insert") &&
            AZURE_AI_SEARCH_INDEX_NAME != null && !AZURE_AI_SEARCH_INDEX_NAME.StartsWith("<insert");
        {{endif}}

        {{if {_IS_WITH_DATA_TEMPLATE}}}
        var ok = azureOk && searchOk &&
        {{else}}
        var ok = azureOk &&
        {{endif}}
            AZURE_OPENAI_SYSTEM_PROMPT != null && !AZURE_OPENAI_SYSTEM_PROMPT.StartsWith("<insert");

        if (!ok)
        {
            Console.WriteLine(
                "To use Azure OpenAI, set the following environment variables:\n" +
                {{if {_IS_WITH_DATA_TEMPLATE}}}
                "\n  AZURE_AI_SEARCH_KEY" +
                "\n  AZURE_AI_SEARCH_ENDPOINT" +
                "\n  AZURE_AI_SEARCH_INDEX_NAME" +
                {{endif}}
                "\n  AZURE_OPENAI_SYSTEM_PROMPT" +
                "\n  AZURE_OPENAI_API_KEY" +
                {{if {_IS_WITH_DATA_TEMPLATE}}}
                "\n  AZURE_OPENAI_API_VERSION" +
                {{endif}}
                "\n  AZURE_OPENAI_CHAT_DEPLOYMENT" +
                "\n  AZURE_OPENAI_ENDPOINT"
            );
            Console.WriteLine(
                "\nYou can easily do that using the Azure AI CLI by doing one of the following:\n" +
                "\n  ai init" +
                "\n  ai dev shell" +
                "\n  dotnet run" +
                "\n" +
                "\n  or" +
                "\n" +
                "\n  ai init" +
                "\n  ai dev shell --run \"dotnet run\""
            );

            return 1;
        }