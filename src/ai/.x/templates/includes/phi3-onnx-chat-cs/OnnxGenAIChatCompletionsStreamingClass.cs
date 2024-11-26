{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}
  {{set indent="        "}}
{{else}}
  {{set indent="    "}}
{{endif}}
using System.Text;
using Microsoft.ML.OnnxRuntimeGenAI;
{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}
using System.Text.Json;
{{else}}
public class ContentMessage
{
    public ContentMessage()
    {
        Role = string.Empty;
        Content = string.Empty;
    }

    public string Role { get; set; }
    public string Content { get; set; }
}
{{endif}}

public class OnnxGenAIChatStreamingClass
{
    public OnnxGenAIChatStreamingClass(string modelDirectory, string systemPrompt{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}, FunctionFactory factory{{endif}})
    {
{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}
        systemPrompt = UpdateSystemPrompt(systemPrompt, factory);

{{endif}}
        _modelDirectory = modelDirectory;
        _systemPrompt = systemPrompt;
{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}
        _factory = factory;
{{endif}}

        _messages = new List<{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}OnnxGenAIChatContentMessage{{else}}ContentMessage{{endif}}>();
        _messages.Add(new {{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}OnnxGenAIChatContentMessage{{else}}ContentMessage{{endif}} { Role = "system", Content = _systemPrompt });

        _model = new Model(_modelDirectory);
        _tokenizer = new Tokenizer(_model);
{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}

        _functionCallContext = new OnnxGenAIChatFunctionCallContext(_factory, _messages);
{{endif}}
    }

    public void ClearMessages()
    {
        _messages.Clear();
        _messages.Add(new {{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}OnnxGenAIChatContentMessage{{else}}ContentMessage{{endif}} { Role = "system", Content = _systemPrompt });
    }

    public string GetChatCompletionStreaming(string userPrompt, Action<string>? callback = null)
    {
{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}
        var debug = Environment.GetEnvironmentVariable("DEBUG") != null;

{{endif}}
        _messages.Add(new {{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}OnnxGenAIChatContentMessage{{else}}ContentMessage{{endif}} { Role = "user", Content = userPrompt });

        var responseContent = string.Empty;
{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}
        while (true)
        {
            var history = string.Join("\n", _messages
                .Select(m => $"<|{m.Role}|>\n{m.Content}\n<|end|>"))
                + "<|assistant|>\n";

            using var tokens = _tokenizer.Encode(history);
{{else}}
{indent}    using var tokens = _tokenizer.Encode(string.Join("\n", _messages
{indent}        .Select(m => $"<|{m.Role}|>\n{m.Content}\n<|end|>"))
{indent}        + "<|assistant|>\n");
{{endif}}

{indent}    using var generatorParams = new GeneratorParams(_model);
{indent}    generatorParams.SetSearchOption("max_length", 2048);
{indent}    generatorParams.SetInputSequences(tokens);

{indent}    using var generator = new Generator(_model, generatorParams);

{indent}    var sb = new StringBuilder();
{indent}    while (!generator.IsDone())
{indent}    {
{indent}        generator.ComputeLogits();
{indent}        generator.GenerateNextToken();

{indent}        var outputTokens = generator.GetSequence(0);
{indent}        var newToken = outputTokens.Slice(outputTokens.Length - 1, 1);

{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}
                var startAnswerAt = sb.ToString().LastIndexOf("<|answer|>");
                var endAnswerAt = sb.ToString().LastIndexOf("<|end_answer|>");
                var insideAnswer = startAnswerAt >= 0 && startAnswerAt > endAnswerAt;

{{endif}}
{indent}        var output = _tokenizer.Decode(newToken);
{indent}        sb.Append(output);

{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}
                if (insideAnswer || debug) callback?.Invoke(output);

                if (sb.ToString().Contains("<|end_answer|>")) break;
                if (_functionCallContext.CheckForFunctions(sb)) break;
{{else}}
{indent}        callback?.Invoke(output);
{{endif}}
{indent}    }

{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}
            if (_functionCallContext.TryCallFunctions(sb))
            {
                _functionCallContext.Clear();
                continue;
            }

{{endif}}
{indent}    responseContent = sb.ToString();
{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}
            var ok = !string.IsNullOrWhiteSpace(responseContent);
            if (ok)
            {
{{endif}}
{indent}{indent}_messages.Add(new {{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}OnnxGenAIChatContentMessage{{else}}ContentMessage{{endif}} { Role = "assistant", Content = responseContent });
{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}
            }
{{endif}}

{indent}    return responseContent;
{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}
        }
{{endif}}
    }

{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}
    private static string UpdateSystemPrompt(string systemPrompt, FunctionFactory factory)
    {
        var functionsSchemaStartsAt = systemPrompt.IndexOf("<|functions_schema|>");
        if (functionsSchemaStartsAt >= 0)
        {
            var functionsSchemaEndsAt = systemPrompt.IndexOf("<|end_functions_schema|>", functionsSchemaStartsAt);
            if (functionsSchemaEndsAt >= 0)
            {
                var asYaml = new StringBuilder();
                var tools = factory.GetChatTools().ToList();
                foreach (var tool in tools)
                {
                    asYaml.Append($"- name: {tool.Name}\n");
                    asYaml.Append($"  description: {tool.Description}\n");
                    asYaml.Append($"  parameters: |\n");
                    asYaml.Append($"    {tool.Parameters}\n");
                }

                systemPrompt = systemPrompt.Remove(functionsSchemaStartsAt, functionsSchemaEndsAt - functionsSchemaStartsAt + "<|end_functions_schema|>".Length);

                var newFunctionsSchema = "<|functions_schema|>\n" + asYaml + "\n<|end_functions_schema|>";
                systemPrompt = systemPrompt.Insert(functionsSchemaStartsAt, newFunctionsSchema);
            }
        }

        return systemPrompt;
    }

{{endif}}
    private string _modelDirectory;
    private string _systemPrompt;
{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}
    private FunctionFactory _factory;
    private OnnxGenAIChatFunctionCallContext _functionCallContext;
{{endif}}
    private Model _model;
    private Tokenizer _tokenizer;
    private List<{{if {_IS_PHI3_ONNX_CHAT_FUNCTIONS_TEMPLATE}}}OnnxGenAIChatContentMessage{{else}}ContentMessage{{endif}}> _messages;
}
