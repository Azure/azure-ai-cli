<#@ template hostspecific="true" #>
<#@ output extension=".go" encoding="utf-8" #>
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

func main() {
	azureOpenAIKey := os.Getenv("AZURE_OPENAI_KEY")
	if azureOpenAIKey == "" {
		azureOpenAIKey = "<#= AZURE_OPENAI_KEY #>"
	}
	azureOpenAIEndpoint := os.Getenv("AZURE_OPENAI_ENDPOINT")
	if azureOpenAIEndpoint == "" {
		azureOpenAIEndpoint = "<#= AZURE_OPENAI_ENDPOINT #>"
	}
	modelDeploymentID := os.Getenv("AZURE_OPENAI_CHAT_DEPLOYMENT")
	if modelDeploymentID == "" {
		modelDeploymentID = "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>"
	}
	systemPrompt := os.Getenv("OPENAI_SYSTEM_PROMPT")
	if systemPrompt == "" {
		systemPrompt = "<#= AZURE_OPENAI_SYSTEM_PROMPT #>"
	}

	keyCredential, err := azopenai.NewKeyCredential(azureOpenAIKey)
	if err != nil {
		log.Fatalf("ERROR: %s", err)
	}
	client, err := azopenai.NewClientWithKeyCredential(azureOpenAIEndpoint, keyCredential, nil)
	if err != nil {
		log.Fatalf("ERROR: %s", err)
	}

	options := azopenai.ChatCompletionsOptions{
		Deployment: modelDeploymentID,
		Messages: []azopenai.ChatMessage{
			{Role: to.Ptr(azopenai.ChatRoleSystem), Content: to.Ptr(systemPrompt)},
		},
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

		options.Messages = append(options.Messages, azopenai.ChatMessage{Role: to.Ptr(azopenai.ChatRoleUser), Content: to.Ptr(userPrompt)})

		resp, err := client.GetChatCompletionsStream(context.TODO(), options, nil)
		if err != nil {
			log.Fatalf("ERROR: %s", err)
		}
		defer resp.ChatCompletionsStream.Close()

		responseContent := ""
		for {
			chatCompletions, err := resp.ChatCompletionsStream.Read()
			if errors.Is(err, io.EOF) {
				break
			}
			if err != nil {
				//  TODO: Update the following line with your application specific error handling logic
				log.Fatalf("ERROR: %s", err)
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

				// TODO: switch to callback
				fmt.Printf("%s", content)
				responseContent += content
			}
		}

		options.Messages = append(options.Messages, azopenai.ChatMessage{Role: to.Ptr(azopenai.ChatRoleAssistant), Content: to.Ptr(responseContent)})
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
