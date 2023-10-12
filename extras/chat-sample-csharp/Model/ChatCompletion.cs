// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.AI.Chat.SampleService;

public struct ChatCompletion
{
    [JsonPropertyName("choices")]
    public List<ChatChoice> Choices { get; set; }
}
