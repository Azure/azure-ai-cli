using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Azure.AI.OpenAI;
using Newtonsoft.Json.Linq;
using DevLab.JmesPath.Utils;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class FunctionDefinitionAttribute : Attribute
    {
        public string Description { get; set; }
    }

    public static class Calculator
    {
        [FunctionDefinition(Description = "Returns the age of a person")]
        public static int GetPersonAge(string personName)
        {
            return personName switch
            {
                "Beckett" => 17,
                "Jac" => 19,
                "Kris" => 53,
                "Rob" => 54,
                _ => 0,
            };
        }

        [FunctionDefinition(Description = "Adds two numbers")]
        public static int Add(int a, int b)
        {
            return a + b;
        }

        [FunctionDefinition(Description = "Subtracts two numbers")]
        public static int Subtract(int a, int b)
        {
            return a - b;
        }

        [FunctionDefinition(Description = "Multiplies two numbers")]
        public static int Multiply(int a, int b)
        {
            return a * b;
        }

        [FunctionDefinition(Description = "Divides two numbers")]
        public static int Divide(int a, int b)
        {
            return a / b;
        }
    }

    public class FunctionFactory
    {
        public FunctionFactory()
        {
        }

        public FunctionFactory(Assembly assembly)
        {
            AddFunctions(assembly);
        }

        public FunctionFactory(Type type1, params Type[] types)
        {
            AddFunctions(type1, types);
        }

        public FunctionFactory(IEnumerable<Type> types)
        {
            AddFunctions(types);
        }

        public FunctionFactory(Type type)
        {
            AddFunctions(type);
        }

        public void AddFunctions(Assembly assembly)
        {
            AddFunctions(assembly.GetTypes());
        }

        public void AddFunctions(Type type1, params Type[] types)
        {
            AddFunctions(new List<Type> { type1 });
            AddFunctions(types);
        }

        public void AddFunctions(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                AddFunctions(type);
            }
        }

        public void AddFunctions(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (var method in methods)
            {
                AddFunction(method);
            }
        }

        public void AddFunction(MethodInfo method)
        {
            var attributes = method.GetCustomAttributes(typeof(FunctionDefinitionAttribute), false);
            if (attributes.Length > 0)
            {
                var functionDefinition = new FunctionDefinition();
                functionDefinition.Name = method.Name;
                functionDefinition.Description = ((FunctionDefinitionAttribute)attributes[0]).Description;

                var required = new JArray();
                var parameters = new JObject();
                parameters["type"] = "object";
                parameters["properties"] = new JObject();

                foreach (var parameter in method.GetParameters())
                {
                    var parameterName = parameter.Name;
                    required.Add(parameterName);

                    var parameterType = parameter.ParameterType.Name switch
                    {
                        "String" => "string",
                        "Int32" => "integer",
                        _ => parameter.ParameterType.Name,
                    };

                    var parameterJson = new JObject();
                    parameterJson["type"] = parameterType;
                    parameterJson["description"] = $"The {parameterName} parameter";

                    parameters["properties"][parameterName] = parameterJson;
                }

                parameters["required"] = required;

                var json = parameters.ToString(Formatting.None);
                functionDefinition.Parameters = new BinaryData(json);

                _functions.Add(method, functionDefinition);
            }
        }

        public void AddTo(ChatCompletionsOptions options)
        {
        }

        public IEnumerable<FunctionDefinition> GetFunctionDefinitions()
        {
            return _functions.Values;
        }

        public static implicit operator List<FunctionDefinition>(FunctionFactory functionFactory)
        {
            return functionFactory._functions.Values.ToList();
        }

        public bool TryCallFunction(ChatCompletionsOptions options, StreamingFunctionCallContext context)
        {
            if (context.FunctionName != null && !string.IsNullOrEmpty(context.Arguments))
            {
                var function = _functions.FirstOrDefault(x => x.Value.Name == context.FunctionName);
                if (function.Key != null)
                {
                    var result = CallFunction(function.Key, function.Value, context.Arguments);
                    options.Messages.Add(new ChatMessage() { Role = ChatRole.Assistant, FunctionCall = new FunctionCall(context.FunctionName, context.Arguments) });
                    options.Messages.Add(new ChatMessage(ChatRole.Function, result) { Name = context.FunctionName });
                    return true;
                }
            }
            return false;
        }

        private static string CallFunction(MethodInfo methodInfo, FunctionDefinition functionDefinition, string argumentsAsJson)
        {
            var jObject = JObject.Parse(argumentsAsJson);
            var arguments = new List<object>();

            var schema = functionDefinition.Parameters.ToString();
            var schemaObject = JObject.Parse(schema);

            var parameters = methodInfo.GetParameters();
            foreach (var parameter in parameters)
            {
                var parameterName = parameter.Name;
                var parameterType = parameter.ParameterType.Name;

                var parameterValue = jObject[parameterName].ToString();

                if (parameterType == "String")
                {
                    arguments.Add(parameterValue);
                }
                else if (parameterType == "Int32")
                {
                    arguments.Add(int.Parse(parameterValue));
                }
                else
                {
                    var isRequired = schemaObject["required"].Values<string>().Contains(parameterName);
                    if (isRequired) throw new Exception($"Unknown parameter type: {parameterType}");
                }
            }

            var result = methodInfo.Invoke(null, arguments.ToArray());
            return result.ToString();
        }

        private Dictionary<MethodInfo, FunctionDefinition> _functions = new();
    }

    public class StreamingFunctionCallContext
    {
        public bool CheckForUpdate(StreamingChatCompletionsUpdate update)
        {
            var updated = false;

            if (!string.IsNullOrEmpty(update.FunctionName))
            {
                _functionName = update.FunctionName;
                updated = true;
            }
            
            var arguments = update.FunctionArgumentsUpdate;
            if (arguments != null)
            {
                _arguments += arguments;
                updated = true;
            }

            return updated;
        }

        public void Reset()
        {
            _functionName = null;
            _arguments = string.Empty;
        }

        public string FunctionName => _functionName;

        public string Arguments => _arguments;

        private string _functionName = null;
        private string _arguments = string.Empty;
    }

    public static class FunctionFactoryExtensions
    {
        // extension method to ChatCompletionsOptions
        public static StreamingFunctionCallContext AddFunctions(this ChatCompletionsOptions options, FunctionFactory functionFactory)
        {
            foreach (var function in functionFactory.GetFunctionDefinitions())
            {
                options.Functions.Add(function);
            }

            return new StreamingFunctionCallContext();
        }

        public static bool TryCallFunction(this ChatCompletionsOptions options, FunctionFactory funcFactory, StreamingFunctionCallContext context)
        {
            return funcFactory.TryCallFunction(options, context);
        }
    }
}
