package main

import (
    "bufio"
    "fmt"
    "log"
    "os"
    "strings"
)

func main() {
    openAIAPIKey := os.Getenv("AZURE_OPENAI_API_KEY")
    if openAIAPIKey == "" {
        openAIAPIKey = "{AZURE_OPENAI_API_KEY}"
    }
    openAIApiVersion := os.Getenv("AZURE_OPENAI_API_VERSION")
    if openAIApiVersion == "" {
        openAIApiVersion = "{AZURE_OPENAI_API_VERSION}"
    }
    openAIEndpoint := os.Getenv("AZURE_OPENAI_ENDPOINT")
    if openAIEndpoint == "" {
        openAIEndpoint = "{AZURE_OPENAI_ENDPOINT}"
    }
    openAIChatDeploymentName := os.Getenv("AZURE_OPENAI_CHAT_DEPLOYMENT")
    if openAIChatDeploymentName == "" {
        openAIChatDeploymentName = "{AZURE_OPENAI_CHAT_DEPLOYMENT}"
    }
    openAISystemPrompt := os.Getenv("AZURE_OPENAI_SYSTEM_PROMPT")
    if openAISystemPrompt == "" {
        openAISystemPrompt = "{AZURE_OPENAI_SYSTEM_PROMPT}"
    }

    openAIEmbeddingsDeploymentName := os.Getenv("AZURE_OPENAI_EMBEDDING_DEPLOYMENT")
    if openAIEmbeddingsDeploymentName == "" {
        openAIEmbeddingsDeploymentName = "{AZURE_OPENAI_EMBEDDING_DEPLOYMENT}"
    }

    openAIEndpoint = strings.TrimSuffix(openAIEndpoint, "/")
    
    azureSearchApiKey := os.Getenv("AZURE_AI_SEARCH_KEY")
    if azureSearchApiKey == "" {
        azureSearchApiKey = "{AZURE_AI_SEARCH_KEY}"
    }

    azureSearchEndpoint := os.Getenv("AZURE_AI_SEARCH_ENDPOINT")
    if azureSearchEndpoint == "" {
        azureSearchEndpoint = "{AZURE_AI_SEARCH_ENDPOINT}"
    }    

    azureSearchIndexName := os.Getenv("AZURE_AI_SEARCH_INDEX_NAME")
    if azureSearchIndexName == "" {
        azureSearchIndexName = "{AZURE_AI_SEARCH_INDEX_NAME}"
    }

    if openAIEndpoint == "" || openAIAPIKey == "" || openAIChatDeploymentName == "" || openAISystemPrompt == "" {
        fmt.Println("Please set the environment variables.")
        os.Exit(1)
    }

    chat, err := New{ClassName}(openAIEndpoint, openAIAPIKey, openAIChatDeploymentName, openAISystemPrompt, azureSearchEndpoint, azureSearchApiKey, azureSearchIndexName, openAIEmbeddingsDeploymentName)
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
