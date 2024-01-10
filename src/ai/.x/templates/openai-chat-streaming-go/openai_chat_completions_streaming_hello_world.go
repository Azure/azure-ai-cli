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
    "context"
    "errors"
    "fmt"
    "io"
    "log"
    "os"
    "strings"

    "github.com/Azure/azure-sdk-for-go/sdk/ai/azopenai"
    "github.com/Azure/azure-sdk-for-go/sdk/azcore/to"
)

type <#= ClassName #> struct {
    client   *azopenai.Client
    options  azopenai.ChatCompletionsOptions
}

func New<#= ClassName #>(systemPrompt, endpoint, azureApiKey, deploymentName string) (*<#= ClassName #>, error) {
    keyCredential, err := azopenai.NewKeyCredential(azureApiKey)
    if err != nil {
        return nil, err
    }
    client, err := azopenai.NewClientWithKeyCredential(endpoint, keyCredential, nil)
    if err != nil {
        return nil, err
    }
    
    return &<#= ClassName #> {
        client: client,
        options: azopenai.ChatCompletionsOptions{
            Deployment: deploymentName,
            Messages: []azopenai.ChatMessage{
                {Role: to.Ptr(azopenai.ChatRoleSystem), Content: to.Ptr(systemPrompt)},
            },
        },
    }, nil
}

func (chat *<#= ClassName #>) ClearConversation() {
    chat.options.Messages = chat.options.Messages[:1]
}

func (chat *<#= ClassName #>) GetChatCompletionsStream(userPrompt string, callback func(content string)) error {
    chat.options.Messages = append(chat.options.Messages, azopenai.ChatMessage{Role: to.Ptr(azopenai.ChatRoleUser), Content: to.Ptr(userPrompt)})

    resp, err := chat.client.GetChatCompletionsStream(context.TODO(), chat.options, nil)
    if err != nil {
        return err
    }
    defer resp.ChatCompletionsStream.Close()

    responseContent := ""
    for {
        chatCompletions, err := resp.ChatCompletionsStream.Read()
        if errors.Is(err, io.EOF) {
            break
        }
        if err != nil {
            return err
        }

        for _, choice := range chatCompletions.Choices {

            content := ""
            if choice.Delta.Content != nil {
                content = *choice.Delta.Content
            }

            if choice.FinishReason != nil {
                finishReason := *choice.FinishReason
                if finishReason == azopenai.CompletionsFinishReasonLength {
                    content = content + "\nWARNING: Exceeded token limit!"
                }
            }

            if content == "" {
                continue
            }

            callback(content)
            responseContent += content
        }
    }

    chat.options.Messages = append(chat.options.Messages, azopenai.ChatMessage{Role: to.Ptr(azopenai.ChatRoleAssistant), Content: to.Ptr(responseContent)})
    return nil
}

func main() {
    azureOpenAIKey := os.Getenv("AZURE_OPENAI_KEY")
    if azureOpenAIKey == "" {
        azureOpenAIKey = "<#= AZURE_OPENAI_KEY #>"
    }
    azureOpenAIEndpoint := os.Getenv("AZURE_OPENAI_ENDPOINT")
    if azureOpenAIEndpoint == "" {
        azureOpenAIEndpoint = "<#= AZURE_OPENAI_ENDPOINT #>"
    }
    deploymentName := os.Getenv("AZURE_OPENAI_CHAT_DEPLOYMENT")
    if deploymentName == "" {
        deploymentName = "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>"
    }
    systemPrompt := os.Getenv("OPENAI_SYSTEM_PROMPT")
    if systemPrompt == "" {
        systemPrompt = "<#= AZURE_OPENAI_SYSTEM_PROMPT #>"
    }

    chat, err := New<#= ClassName #>(systemPrompt, azureOpenAIEndpoint, azureOpenAIKey, deploymentName)
    if err != nil {
        log.Fatalf("ERROR: %s", err)
    }

    callback := func(content string) {
        fmt.Printf("%s", content)
    }

    for {
        fmt.Print("User: ")

        userPrompt, err := getUserInput()
        if err != nil {
            fmt.Println("Error reading input:", err)
            break
        }
        if userPrompt == "exit" || userPrompt == "" {
            break
        }

        fmt.Printf("\nAssistant: ")

        err = chat.GetChatCompletionsStream(userPrompt, callback)
        if err != nil {
            log.Fatalf("ERROR: %s", err)
        }
        fmt.Println("\n")
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
