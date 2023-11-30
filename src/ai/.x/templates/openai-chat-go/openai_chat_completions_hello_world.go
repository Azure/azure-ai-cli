<#@ template hostspecific="true" #>
<#@ output extension=".go" encoding="utf-8" #>
<#@ parameter type="System.String" name="OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
package main

import (
	"bufio"
	"context"
	"fmt"
	"log"
	"os"
	"strings"

	"github.com/Azure/azure-sdk-for-go/sdk/ai/azopenai"
	"github.com/Azure/azure-sdk-for-go/sdk/azcore/to"
)

func main() {
	azureOpenAIKey := os.Getenv("OPENAI_API_KEY")
	if azureOpenAIKey == "" {
		azureOpenAIKey = "<#= OPENAI_API_KEY #>"
	}

	azureOpenAIEndpoint := os.Getenv("OPENAI_ENDPOINT")
	if azureOpenAIEndpoint == "" {
		azureOpenAIEndpoint = "<#= OPENAI_ENDPOINT #>"
	}

	keyCredential, err := azopenai.NewKeyCredential(azureOpenAIKey)
	if err != nil {
		log.Fatalf("ERROR: %s", err)
	}

	client, err := azopenai.NewClientWithKeyCredential(azureOpenAIEndpoint, keyCredential, nil)
	if err != nil {
		log.Fatalf("ERROR: %s", err)
	}

	modelDeploymentID := os.Getenv("AZURE_OPENAI_CHAT_DEPLOYMENT")
	if modelDeploymentID == "" {
		modelDeploymentID = "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>"
	}

	systemPrompt := os.Getenv("OPENAI_SYSTEM_PROMPT")
	if systemPrompt == "" {
		systemPrompt = "<#= AZURE_OPENAI_SYSTEM_PROMPT #>"
	}

	messages := []azopenai.ChatMessage{
		{Role: to.Ptr(azopenai.ChatRoleSystem), Content: to.Ptr(systemPrompt)},
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

		messages = append(messages, azopenai.ChatMessage{Role: to.Ptr(azopenai.ChatRoleUser), Content: to.Ptr(userPrompt)})
		options := azopenai.ChatCompletionsOptions{
			Messages:   messages,
			Deployment: modelDeploymentID,
		}

		resp, err := client.GetChatCompletions(context.TODO(), options, nil)
		if err != nil {
			log.Fatalf("ERROR: %s", err)
		}

		responseContent := *resp.Choices[0].Message.Content
		messages = append(messages, azopenai.ChatMessage{Role: to.Ptr(azopenai.ChatRoleAssistant), Content: to.Ptr(responseContent)})

		fmt.Printf("Assistant: %s\n", responseContent)
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
