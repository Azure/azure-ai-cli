package main

import (
	"fmt"

	"github.com/Azure/azure-sdk-for-go/sdk/ai/azopenai"
	"github.com/Azure/azure-sdk-for-go/sdk/azcore/to"
)

type FunctionCallContext struct {
	functionFactory   *FunctionFactory
	messages          []azopenai.ChatMessage
	functionName      string
	functionArguments string
}

func NewFunctionCallContext(functionFactory *FunctionFactory, messages []azopenai.ChatMessage) *FunctionCallContext {
	return &FunctionCallContext{
		functionFactory:   functionFactory,
		messages:          messages,
		functionName:      "",
		functionArguments: "",
	}
}

func (fcc *FunctionCallContext) CheckForUpdate(choice azopenai.ChatChoice) bool {
	updated := false

	if choice.Delta != nil && choice.Delta.FunctionCall != nil {
		name := choice.Delta.FunctionCall.Name
		if name != nil && *name != "" {
			fcc.functionName = *name
			updated = true
		}
	}

	if choice.Delta != nil && choice.Delta.FunctionCall != nil {
		args := choice.Delta.FunctionCall.Arguments
		if args != nil && *args != "" {
			fcc.functionArguments = *args
			updated = true
		}
	}

	return updated
}

func (fcc *FunctionCallContext) TryCallFunction() string {
	result := fcc.functionFactory.TryCallFunction(fcc.functionName, fcc.functionArguments)
	if result == "" {
		return ""
	}

	fmt.Printf("\rassistant-function: %s(%s) => %s\n", fcc.functionName, fcc.functionArguments, result)

	fcc.messages = append(fcc.messages, azopenai.ChatMessage{Role: to.Ptr(azopenai.ChatRoleAssistant), FunctionCall: &azopenai.ChatMessageFunctionCall{Name: to.Ptr(fcc.functionName), Arguments: to.Ptr(fcc.functionArguments)}})
	fcc.messages = append(fcc.messages, azopenai.ChatMessage{Role: to.Ptr(azopenai.ChatRoleFunction), Content: to.Ptr(result), Name: to.Ptr(fcc.functionName)})

	return result
}

func (fcc *FunctionCallContext) Clear() {
	fcc.functionName = ""
	fcc.functionArguments = ""
}
