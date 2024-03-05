//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;

public class FunctionCallContext
{
    private FunctionFactory _functionFactory;
    private IList<ChatRequestMessage> _messages;
    private string _functionName = "";
    private string _functionArguments = "";

    public FunctionCallContext(FunctionFactory functionFactory, IList<ChatRequestMessage> messages)
    {
        _functionFactory = functionFactory;
        _messages = messages;
    }

    
    public bool CheckForUpdate(StreamingChatCompletionsUpdate update)
    {
        var updated = false;

        var name = update?.FunctionName;
        if (name != null)
        {
            _functionName = name;
            updated = true;
        }

        var args = update?.FunctionArgumentsUpdate;
        if (args != null)
        {
            _functionArguments += args;
            updated = true;
        }

        return updated;
    }

    public string? TryCallFunction()
    {
        var ok = _functionFactory.TryCallFunction(_functionName, _functionArguments, out var result);
        if (!ok) return null;

        Console.WriteLine($"\rassistant-function: {_functionName}({_functionArguments}) => {result}");
        Console.Write("\nAssistant: ");

        _messages.Add(new ChatRequestAssistantMessage("") { FunctionCall = new FunctionCall(_functionName, _functionArguments) });
        _messages.Add(new ChatRequestFunctionMessage(_functionName, result));

        return result;
    }

    public void Clear()
    {
        _functionName = "";
        _functionArguments = "";
    }
}