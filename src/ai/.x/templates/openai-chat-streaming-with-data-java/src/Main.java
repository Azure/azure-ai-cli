<#@ template hostspecific="true" #>
<#@ output extension=".java" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_API_VERSION" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_EMBEDDING_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_KEY" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_INDEX_NAME" #>
import java.util.Scanner;
import reactor.core.publisher.Flux;
import com.azure.ai.openai.models.ChatCompletions;

public class Main {

    public static void main(String[] args) {
        String openAIKey = (System.getenv("AZURE_OPENAI_KEY") != null) ? System.getenv("AZURE_OPENAI_KEY") : "<insert your Azure OpenAI API key here>";
        String openAIEndpoint = (System.getenv("AZURE_OPENAI_ENDPOINT") != null) ? System.getenv("AZURE_OPENAI_ENDPOINT") : "<insert your Azure OpenAI endpoint here>";
        String openAIChatDeployment = (System.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT") != null) ? System.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT") : "<insert your Azure OpenAI chat deployment name here>";
        String openAISystemPrompt = (System.getenv("AZURE_OPENAI_SYSTEM_PROMPT") != null) ? System.getenv("AZURE_OPENAI_SYSTEM_PROMPT") : "You are a helpful AI assistant.";

        String openAIApiVersion = System.getenv("AZURE_OPENAI_API_VERSION") != null ? System.getenv("AZURE_OPENAI_API_VERSION") : "<#= AZURE_OPENAI_API_VERSION #>";
        String azureSearchEmbeddingsDeploymentName = System.getenv("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") != null ? System.getenv("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") : "<#= AZURE_OPENAI_EMBEDDING_DEPLOYMENT #>";
        String azureSearchEndpoint = System.getenv("AZURE_AI_SEARCH_ENDPOINT") != null ? System.getenv("AZURE_AI_SEARCH_ENDPOINT") : "<#= AZURE_AI_SEARCH_ENDPOINT #>";
        String azureSearchAPIKey = System.getenv("AZURE_AI_SEARCH_KEY") != null ? System.getenv("AZURE_AI_SEARCH_KEY") : "<#= AZURE_AI_SEARCH_KEY #>";
        String azureSearchIndexName = System.getenv("AZURE_AI_SEARCH_INDEX_NAME") != null ? System.getenv("AZURE_AI_SEARCH_INDEX_NAME") : "<#= AZURE_AI_SEARCH_INDEX_NAME #>";

        <#= ClassName #> chat = new <#= ClassName #>(openAIKey, openAIEndpoint, openAIChatDeployment, openAISystemPrompt, azureSearchEndpoint, azureSearchIndexName, azureSearchAPIKey, azureSearchEmbeddingsDeploymentName);

        Scanner scanner = new Scanner(System.in);
        while (true) {
            System.out.print("User: ");
            String userPrompt = scanner.nextLine();
            if (userPrompt.isEmpty() || "exit".equals(userPrompt))
                break;

            System.out.print("\nAssistant: ");
            Flux<ChatCompletions> responseFlux = chat.getChatCompletionsStreamingAsync(userPrompt, update -> {
                System.out.print(update.getContent());
            });
            responseFlux.blockLast();
            System.out.println("\n");
        }
        scanner.close();
    }
}