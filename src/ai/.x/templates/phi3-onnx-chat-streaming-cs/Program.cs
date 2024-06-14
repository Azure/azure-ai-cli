using System.Text;
using Microsoft.ML.OnnxRuntimeGenAI;

public class ContentMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
}

public class Program
{
    public static void Main(string[] args)
    {
        var modelDirectory = args.Length < 2
            ? @"Phi-3-mini-4k-instruct-onnx\cpu_and_mobile\cpu-int4-rtn-block-32"
            : args[1];

        using var model = new Model(modelDirectory);
        using var tokenizer = new Tokenizer(model);

        var messages = new List<ContentMessage>();
        messages.Append(new ContentMessage { Role = "system", Content = "You are a helpful assistant." });

        while (true)
        {
            Console.Write("User: ");
            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input) || input == "exit") break;

            Console.Write("\nAssistant: ");

            messages.Add(new ContentMessage { Role = "user", Content = input });
            var asStr = string.Join("\n", messages
                .Select(m => $"<|{m.Role}|>{m.Content}<|end|>"))
                + "<|assistant|>";

            using var tokens = tokenizer.Encode(asStr);

            using var generatorParams = new GeneratorParams(model);
            generatorParams.SetSearchOption("max_length", 2048);
            generatorParams.SetInputSequences(tokens);

            using var generator = new Generator(model, generatorParams);

            var sb = new StringBuilder();
            while (!generator.IsDone())
            {
                generator.ComputeLogits();
                generator.GenerateNextToken();

                var outputTokens = generator.GetSequence(0);
                var newToken = outputTokens.Slice(outputTokens.Length - 1, 1);

                var output = tokenizer.Decode(newToken);
                sb.Append(output);

                Console.Write(output);
            }

            Console.WriteLine("\n");
            
            messages.Add(new ContentMessage { Role = "assistant", Content = sb.ToString() });
        }
    }
}
