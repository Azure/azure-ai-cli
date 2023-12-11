<#@ template hostspecific="true" #>
<#@ output extension=".java" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
import com.azure.ai.openai.OpenAIClient;
import com.azure.ai.openai.OpenAIClientBuilder;
import com.azure.ai.openai.models.ChatChoice;
import com.azure.ai.openai.models.ChatCompletions;
import com.azure.ai.openai.models.ChatCompletionsOptions;
import com.azure.ai.openai.models.ChatMessage;
import com.azure.ai.openai.models.ChatRole;
import com.azure.ai.openai.models.CompletionsUsage;
import com.azure.core.credential.AzureKeyCredential;

import java.util.ArrayList;
import java.util.List;
import java.util.Scanner;

public class OpenAIQuickstart {

    private OpenAIClient client;
    private ChatCompletionsOptions options;

    private String key = (System.getenv("OPENAI_API_KEY") != null) ? System.getenv("OPENAI_API_KEY") : "<#= OPENAI_API_KEY #>";
    private String endpoint = (System.getenv("OPENAI_ENDPOINT") != null) ? System.getenv("OPENAI_ENDPOINT") : "<#= OPENAI_ENDPOINT #>";
    private String deploymentName = (System.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT") != null) ? System.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT") : "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>";
    private String systemPrompt = (System.getenv("AZURE_OPENAI_SYSTEM_PROMPT") != null) ? System.getenv("AZURE_OPENAI_SYSTEM_PROMPT") : "<#= AZURE_OPENAI_SYSTEM_PROMPT #>";

    public OpenAIQuickstart() {

        client = new OpenAIClientBuilder()
            .endpoint(endpoint)
            .credential(new AzureKeyCredential(key))
            .buildClient();

        List<ChatMessage> chatMessages = new ArrayList<>();
        chatMessages.add(new ChatMessage(ChatRole.SYSTEM, systemPrompt));

        options = new ChatCompletionsOptions(chatMessages);
    }

    public String getChatCompletion(String userPrompt) {
        options.getMessages().add(new ChatMessage(ChatRole.USER, userPrompt));

        ChatCompletions chatCompletions = client.getChatCompletions(deploymentName, options);
        String responseContent = chatCompletions.getChoices().get(0).getMessage().getContent();
        options.getMessages().add(new ChatMessage(ChatRole.ASSISTANT, responseContent));

        return responseContent;
    }

    public static void main(String[] args) {
        OpenAIQuickstart chat = new OpenAIQuickstart();

        Scanner scanner = new Scanner(System.in);
        while (true) {
            System.out.print("User: ");
            String userPrompt = scanner.nextLine();
            if (userPrompt.isEmpty() || "exit".equals(userPrompt)) break;

            String response = chat.getChatCompletion(userPrompt);
            System.out.println("\nAssistant: " + response + "\n");
        }
        scanner.close();
    }
}