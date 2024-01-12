<#@ template hostspecific="true" #>
<#@ output extension=".go" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
package main

import (
    "bufio"
    "fmt"
    "log"
    "os"
    "strings"
)

func main() {
    azureOpenAIEndpoint := os.Getenv("AZURE_OPENAI_ENDPOINT")
    if azureOpenAIEndpoint == "" {
        azureOpenAIEndpoint = "<#= AZURE_OPENAI_ENDPOINT #>"
    }
    azureOpenAIKey := os.Getenv("AZURE_OPENAI_KEY")
    if azureOpenAIKey == "" {
        azureOpenAIKey = "<#= AZURE_OPENAI_KEY #>"
    }
    deploymentName := os.Getenv("AZURE_OPENAI_CHAT_DEPLOYMENT")
    if deploymentName == "" {
        deploymentName = "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>"
    }
    systemPrompt := os.Getenv("OPENAI_SYSTEM_PROMPT")
    if systemPrompt == "" {
        systemPrompt = "<#= AZURE_OPENAI_SYSTEM_PROMPT #>"
    }

    if azureOpenAIEndpoint == "" || azureOpenAIKey == "" || deploymentName == "" || systemPrompt == "" {
        fmt.Println("Please set the environment variables.")
        os.Exit(1)
    }

    chat, err := New<#= ClassName #>(systemPrompt, azureOpenAIEndpoint, azureOpenAIKey, deploymentName)
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
