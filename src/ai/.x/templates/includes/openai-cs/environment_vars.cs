        var openAIAPIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "{AZURE_OPENAI_API_KEY}";
{{if {_IS_WITH_DATA_TEMPLATE}}}
        var openAIApiVersion = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_VERSION") ?? "{AZURE_OPENAI_API_VERSION}";
{{endif}} 
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "{AZURE_OPENAI_ENDPOINT}";
        var openAIChatDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT") ?? "{AZURE_OPENAI_CHAT_DEPLOYMENT}";
        var openAISystemPrompt = Environment.GetEnvironmentVariable("AZURE_OPENAI_SYSTEM_PROMPT") ?? "{AZURE_OPENAI_SYSTEM_PROMPT}";
{{if {_IS_WITH_DATA_TEMPLATE}}}

        var openAIEmbeddingsDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? "{AZURE_OPENAI_EMBEDDING_DEPLOYMENT}";
        var openAIEmbeddingsEndpoint = $"{openAIEndpoint.Trim('/')}/openai/deployments/{openAIEmbeddingsDeploymentName}/embeddings?api-version={openAIApiVersion}";

        var searchApiKey = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_KEY") ?? "{AZURE_AI_SEARCH_KEY}";
        var searchEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_ENDPOINT") ?? "{AZURE_AI_SEARCH_ENDPOINT}";
        var searchIndexName = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_INDEX_NAME") ?? "{AZURE_AI_SEARCH_INDEX_NAME}";
{{endif}}