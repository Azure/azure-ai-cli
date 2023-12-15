<#@ template hostspecific="true" #>
<#@ output extension=".java" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
import com.azure.ai.openai.OpenAIAsyncClient;
import com.azure.ai.openai.OpenAIClient;
import com.azure.ai.openai.OpenAIClientBuilder;
import com.azure.ai.openai.models.ChatChoice;
import com.azure.ai.openai.models.ChatCompletions;
import com.azure.ai.openai.models.ChatCompletionsOptions;
import com.azure.ai.openai.models.ChatRequestAssistantMessage;
import com.azure.ai.openai.models.ChatRequestMessage;
import com.azure.ai.openai.models.ChatRequestSystemMessage;
import com.azure.ai.openai.models.ChatRole;
import com.azure.ai.openai.models.ChatRequestUserMessage;
import com.azure.ai.openai.models.ChatResponseMessage;
import com.azure.ai.openai.models.CompletionsUsage;
import com.azure.ai.openai.models.CompletionsFinishReason;
import com.azure.core.credential.AzureKeyCredential;

import reactor.core.publisher.Flux;

import java.time.Duration;
import java.util.ArrayList;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.function.Consumer;
import java.util.List;
import java.util.Scanner;

public class OpenAIQuickstartStreaming {

    private OpenAIAsyncClient client;
    private ChatCompletionsOptions options;

    private String key = (System.getenv("OPENAI_API_KEY") != null) ? System.getenv("OPENAI_API_KEY")
            : "<insert your OpenAI API key here>";
    private String endpoint = (System.getenv("OPENAI_ENDPOINT") != null) ? System.getenv("OPENAI_ENDPOINT")
            : "<insert your OpenAI endpoint here>";
    private String deploymentName = (System.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT") != null)
            ? System.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT")
            : "<insert your OpenAI deployment name here>";
    private String systemPrompt = (System.getenv("AZURE_OPENAI_SYSTEM_PROMPT") != null)
            ? System.getenv("AZURE_OPENAI_SYSTEM_PROMPT")
            : "You are a helpful AI assistant.";

    public OpenAIQuickstartStreaming() {

        client = new OpenAIClientBuilder()
                .endpoint(endpoint)
                .credential(new AzureKeyCredential(key))
                .buildAsyncClient();

        List<ChatRequestMessage> chatMessages = new ArrayList<>();
        chatMessages.add(new ChatRequestSystemMessage(systemPrompt));

        options = new ChatCompletionsOptions(chatMessages);
        options.setStream(true);
    }

    public Flux<ChatCompletions> getChatCompletionsStreamingAsync(String userPrompt,
            Consumer<ChatResponseMessage> callback) {
        options.getMessages().add(new ChatRequestUserMessage(userPrompt));

        StringBuilder responseContent = new StringBuilder();
        Flux<ChatCompletions> response = client.getChatCompletionsStream(deploymentName, options);

        response.subscribe(chatResponse -> {
            if (chatResponse.getChoices() != null) {
                for (ChatChoice update : chatResponse.getChoices()) {
                    if (update.getDelta() == null || update.getDelta().getContent() == null)
                        continue;
                    callback.accept(update.getDelta());
                    String content = update.getDelta().getContent();

                    if (update.getFinishReason() == null)
                        continue;
                    if (update.getFinishReason() == CompletionsFinishReason.CONTENT_FILTERED) {
                        content = content + "\nWARNING: Content filtered!";
                    } else if (update.getFinishReason() == CompletionsFinishReason.TOKEN_LIMIT_REACHED) {
                        content = content + "\nERROR: Exceeded token limit!";
                    }

                    if (content.isEmpty())
                        continue;

                    responseContent.append(content);
                }

                options.getMessages().add(new ChatRequestAssistantMessage(responseContent.toString()));
            }
        });

        return response;
    }

    public static void main(String[] args) {
        OpenAIQuickstartStreaming chat = new OpenAIQuickstartStreaming();

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
            responseFlux.blockLast(Duration.ofSeconds(20));
            System.out.println("\n");
        }
        scanner.close();
    }
}