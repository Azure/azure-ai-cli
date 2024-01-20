<#@ template hostspecific="true" #>
<#@ output extension=".java" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
import com.azure.ai.openai.OpenAIClient;
import com.azure.ai.openai.OpenAIClientBuilder;
import com.azure.ai.openai.models.ChatCompletions;
import com.azure.ai.openai.models.ChatCompletionsOptions;
import com.azure.ai.openai.models.ChatMessage;
import com.azure.ai.openai.models.ChatRole;
import com.azure.core.credential.AzureKeyCredential;

import java.util.ArrayList;
import java.util.List;

public class <#= ClassName #> {

    private OpenAIClient client;
    private ChatCompletionsOptions options;
    private String openAIChatDeployment;
    private String openAISystemPrompt;

    public <#= ClassName #> (String openAIKey, String openAIEndpoint, String openAIChatDeployment, String openAISystemPrompt) {
        this.openAIChatDeployment = openAIChatDeployment;
        this.openAISystemPrompt = openAISystemPrompt;
        client = new OpenAIClientBuilder()
            .endpoint(openAIEndpoint)
            .credential(new AzureKeyCredential(openAIKey))
            .buildClient();

        List<ChatMessage> chatMessages = new ArrayList<>();
        options = new ChatCompletionsOptions(chatMessages);
        ClearConversation();
    }

    public void ClearConversation(){
        List<ChatMessage> chatMessages = options.getMessages();
        chatMessages.clear();
        chatMessages.add(new ChatMessage(ChatRole.SYSTEM, this.openAISystemPrompt));
    }

    public String getChatCompletion(String userPrompt) {
        options.getMessages().add(new ChatMessage(ChatRole.USER, userPrompt));

        ChatCompletions chatCompletions = client.getChatCompletions(this.openAIChatDeployment, options);
        String responseContent = chatCompletions.getChoices().get(0).getMessage().getContent();
        options.getMessages().add(new ChatMessage(ChatRole.ASSISTANT, responseContent));

        return responseContent;
    }
}