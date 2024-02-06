<#@ template hostspecific="true" #>
<#@ output extension=".go" encoding="utf-8" #>
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
package main

import (
    "bufio"
    "fmt"
    "log"
    "os"
    "strings"
)

func main() {
    openAIEndpoint := os.Getenv("AZURE_OPENAI_ENDPOINT")
    if openAIEndpoint == "" {
        openAIEndpoint = "<#= AZURE_OPENAI_ENDPOINT #>"
    }
    openAIKey := os.Getenv("AZURE_OPENAI_KEY")
    if openAIKey == "" {
        openAIKey = "<#= AZURE_OPENAI_KEY #>"
    }
    openAIChatDeploymentName := os.Getenv("AZURE_OPENAI_CHAT_DEPLOYMENT")
    if openAIChatDeploymentName == "" {
        openAIChatDeploymentName = "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>"
    }
    openAISystemPrompt := os.Getenv("OPENAI_SYSTEM_PROMPT")
    if openAISystemPrompt == "" {
        openAISystemPrompt = "<#= AZURE_OPENAI_SYSTEM_PROMPT #>"
    }

    openAIApiVersion := os.Getenv("AZURE_OPENAI_API_VERSION")
    if openAIApiVersion == "" {
        openAIApiVersion = "<#= AZURE_OPENAI_API_VERSION #>"
    }

    openAIEmbeddingsDeploymentName := os.Getenv("AZURE_OPENAI_EMBEDDING_DEPLOYMENT")
    if openAIEmbeddingsDeploymentName == "" {
        openAIEmbeddingsDeploymentName = "<#= AZURE_OPENAI_EMBEDDING_DEPLOYMENT #>"
    }

    openAIEndpoint = strings.TrimSuffix(openAIEndpoint, "/")

    azureSearchEndpoint := os.Getenv("AZURE_AI_SEARCH_ENDPOINT")
    if azureSearchEndpoint == "" {
        azureSearchEndpoint = "<#= AZURE_AI_SEARCH_ENDPOINT #>"
    }

    azureSearchApiKey := os.Getenv("AZURE_AI_SEARCH_KEY")
    if azureSearchApiKey == "" {
        azureSearchApiKey = "<#= AZURE_AI_SEARCH_KEY #>"
    }

    azureSearchIndexName := os.Getenv("AZURE_AI_SEARCH_INDEX_NAME")
    if azureSearchIndexName == "" {
        azureSearchIndexName = "<#= AZURE_AI_SEARCH_INDEX_NAME #>"
    }

    if openAIEndpoint == "" || openAIKey == "" || openAIChatDeploymentName == "" || openAISystemPrompt == "" {
        fmt.Println("Please set the environment variables.")
        os.Exit(1)
    }

    chat, err := New<#= ClassName #>(openAIEndpoint, openAIKey, openAIChatDeploymentName, openAISystemPrompt, azureSearchEndpoint, azureSearchApiKey, azureSearchIndexName, openAIEmbeddingsDeploymentName)
    if err != nil {
        log.Fatalf("ERROR: %s", err)
    }

    for {
        fmt.Print("User: ")
        input, _ := getUserInput()
        if input == "exit" || input == "" {
            break
        }

        fmt.Printf("\nAssistant: ")
        _, err := chat.GetChatCompletionsStream(input, func(content string) {
            fmt.Printf("%s", content)
        })
        if err != nil {
            log.Fatalf("ERROR: %s", err)
        }
        fmt.Printf("\n\n")
    }
}

func getUserInput() (string, error) {
    reader := bufio.NewReader(os.Stdin)
    userInput, err := reader.ReadString('\n')
    if err != nil {
        return "", err
    }
    userInput = strings.TrimSuffix(userInput, "\n")
    userInput = strings.TrimSuffix(userInput, "\r")
    return userInput, nil
}
