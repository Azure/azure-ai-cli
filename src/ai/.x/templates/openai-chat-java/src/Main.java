<#@ template hostspecific="true" #>
<#@ output extension=".java" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
import java.util.Scanner;
public class Main {
    public static void main(String[] args) {
    	String openAIKey = (System.getenv("AZURE_OPENAI_KEY") != null) ? System.getenv("AZURE_OPENAI_KEY") : "<insert your OpenAI API key here>";
        String openAIEndpoint = (System.getenv("AZURE_OPENAI_ENDPOINT") != null) ? System.getenv("AZURE_OPENAI_ENDPOINT") : "<insert your OpenAI endpoint here>";
        String openAIChatDeployment = (System.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT") != null) ? System.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT") : "<insert your OpenAI chat deployment name here>";
        String openAISystemPrompt = (System.getenv("AZURE_OPENAI_SYSTEM_PROMPT") != null) ? System.getenv("AZURE_OPENAI_SYSTEM_PROMPT") : "You are a helpful AI assistant.";

        <#= ClassName #> chat = new <#= ClassName #>(openAIKey, openAIEndpoint, openAIChatDeployment, openAISystemPrompt);

        Scanner scanner = new Scanner(System.in);
        while (true) {
            System.out.print("User: ");
            if (!scanner.hasNextLine()) break;

            String userPrompt = scanner.nextLine();
            if (userPrompt.isEmpty() || "exit".equals(userPrompt)) break;

            String response = chat.getChatCompletion(userPrompt);
            System.out.println("\nAssistant: " + response + "\n");
        }
        scanner.close();
    }
}