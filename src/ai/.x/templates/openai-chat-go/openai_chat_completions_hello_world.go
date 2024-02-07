<#@ template hostspecific="true" #>
<#@ output extension=".go" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
package main

import (
    "context"

    "github.com/Azure/azure-sdk-for-go/sdk/ai/azopenai"
    "github.com/Azure/azure-sdk-for-go/sdk/azcore"
    "github.com/Azure/azure-sdk-for-go/sdk/azcore/to"
)

type <#= ClassName #> struct {
    client   *azopenai.Client
    options  *azopenai.ChatCompletionsOptions
}

func New<#= ClassName #>(openAIEndpoint string, openAIKey string, openAIChatDeploymentName string, openAISystemPrompt string) (*<#= ClassName #>, error) {
    keyCredential := azcore.NewKeyCredential(openAIKey)

    client, err := azopenai.NewClientWithKeyCredential(openAIEndpoint, keyCredential, nil)
    if err != nil {
        return nil, err
    }

    messages := []azopenai.ChatRequestMessageClassification{
        &azopenai.ChatRequestSystemMessage{
            Content: &openAISystemPrompt,
        },
    }

    options := &azopenai.ChatCompletionsOptions{
        DeploymentName: &openAIChatDeploymentName,
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
    chat.options.Messages = append(chat.options.Messages, &azopenai.ChatRequestUserMessage{Content: azopenai.NewChatRequestUserMessageContent(userPrompt)})

    resp, err := chat.client.GetChatCompletions(context.TODO(), *chat.options, nil)
    if err != nil {
        return "", err
    }

    responseContent := *resp.Choices[0].Message.Content
    chat.options.Messages = append(chat.options.Messages, &azopenai.ChatRequestAssistantMessage{Content: to.Ptr(responseContent)})

    return responseContent, nil
}
