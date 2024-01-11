<#@ template hostspecific="true" #>
<#@ output extension=".go" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
package main

import (
	"context"
	"errors"
	"io"

	"github.com/Azure/azure-sdk-for-go/sdk/ai/azopenai"
	"github.com/Azure/azure-sdk-for-go/sdk/azcore/to"
)


type <#= ClassName #> struct {
	systemPrompt       string
	deploymentName     string
	client              *azopenai.Client
    options             azopenai.ChatCompletionsOptions
	functionFactory    *FunctionFactory
	functionCallContext *FunctionCallContext
}

func New<#= ClassName #>(systemPrompt string, endpoint string, azureApiKey string, deploymentName string, functionFactory *FunctionFactory) (*<#= ClassName #>, error) {
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

	return &<#= ClassName #>{
		systemPrompt:   systemPrompt,
		deploymentName: deploymentName,
		client:         client,
		options: azopenai.ChatCompletionsOptions{
			Deployment: deploymentName,
			Messages:   messages,
			FunctionCall: &azopenai.ChatCompletionsOptionsFunctionCall{
				Value: to.Ptr("auto"),
			},
			Functions: functionFactory.GetFunctionSchemas(),
		},
		functionFactory:     functionFactory,
		functionCallContext: NewFunctionCallContext(functionFactory, messages),
	}, nil
}

func (oac *<#= ClassName #>) clearConversation() {
	oac.options.Messages = oac.options.Messages[:1]
	oac.functionCallContext = NewFunctionCallContext(oac.functionFactory, oac.options.Messages)
}

func (chat *<#= ClassName #>) GetChatCompletionsStream(userPrompt string, callback func(content string)) error {
	chat.options.Messages = append(chat.options.Messages, azopenai.ChatMessage{Role: to.Ptr(azopenai.ChatRoleUser), Content: to.Ptr(userPrompt)})

	responseContent := ""
	for {
		resp, err := chat.client.GetChatCompletionsStream(context.TODO(), chat.options, nil)
		if err != nil {
			return err
		}
		defer resp.ChatCompletionsStream.Close()

		for {
			chatCompletions, err := resp.ChatCompletionsStream.Read()
			if errors.Is(err, io.EOF) {
				break
			}
			if err != nil {
				return err
			}

			for _, choice := range chatCompletions.Choices {

				chat.functionCallContext.CheckForUpdate(choice)

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

		if chat.functionCallContext.TryCallFunction() != "" {
			chat.functionCallContext.Clear()
			continue
		}

		chat.options.Messages = append(chat.options.Messages, azopenai.ChatMessage{Role: to.Ptr(azopenai.ChatRoleAssistant), Content: to.Ptr(responseContent)})
		return nil
	}
}
