<#@ template hostspecific="true" #>
<#@ output extension=".go" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
package main

import (
    "context"

    "github.com/Azure/azure-sdk-for-go/sdk/ai/azopenai"
    "github.com/Azure/azure-sdk-for-go/sdk/azcore/to"
)

type <#= ClassName #> struct {
    client   *azopenai.Client
    options  *azopenai.ChatCompletionsOptions
}

func New<#= ClassName #>(systemPrompt string, endpoint string, azureApiKey string, deploymentName string) (*<#= ClassName #>, error) {
    keyCredential, err := azopenai.NewKeyCredential(azureApiKey)
    if err != nil {
        return nil, err
    }
    client, err := azopenai.NewClientWithKeyCredential(endpoint, keyCredential, nil)
    if err != nil {
        return nil, err
    }

    messages := []azopenai.ChatMessage{
        {Role: to.Ptr(azopenai.ChatRoleSystem), Content: to.Ptr(systemPrompt)},
    }

    options := &azopenai.ChatCompletionsOptions{
        Deployment: deploymentName,
        Messages: messages,
    }

    return &<#= ClassName #> {
        client: client,
        options: options,
    }, nil
}

func (chat *<#= ClassName #>) ClearConversation() {
    chat.options.Messages = chat.options.Messages[:1]
}

func (chat *<#= ClassName #>) GetChatCompletions(userPrompt string) (string, error) {
    chat.options.Messages = append(chat.options.Messages, azopenai.ChatMessage{Role: to.Ptr(azopenai.ChatRoleUser), Content: to.Ptr(userPrompt)})

    resp, err := chat.client.GetChatCompletions(context.TODO(), *chat.options, nil)
    if err != nil {
        return "", err
    }

    responseContent := *resp.Choices[0].Message.Content
    chat.options.Messages = append(chat.options.Messages, azopenai.ChatMessage{Role: to.Ptr(azopenai.ChatRoleAssistant), Content: to.Ptr(responseContent)})

    return responseContent, nil
}
