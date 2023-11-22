using System.Reflection;
using Newtonsoft.Json;
using Azure.AI.OpenAI;
using Newtonsoft.Json.Linq;

namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
{
    public class HelperFunctionFactory
    {
        public HelperFunctionFactory()
        {
        }

        public HelperFunctionFactory(Assembly assembly)
        {
            AddFunctions(assembly);
        }

        public HelperFunctionFactory(Type type1, params Type[] types)
        {
            AddFunctions(type1, types);
        }

        public HelperFunctionFactory(IEnumerable<Type> types)
        {
            AddFunctions(types);
        }

        public HelperFunctionFactory(Type type)
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
            var attributes = method.GetCustomAttributes(typeof(HelperFunctionDescriptionAttribute), false);
            if (attributes.Length > 0)
            {
                var funcDescriptionAttrib = attributes[0] as HelperFunctionDescriptionAttribute;
                var funcDescription = funcDescriptionAttrib!.Description;

                string json = GetMethodParametersJsonSchema(method, ref attributes);
                _functions.Add(method, new FunctionDefinition(method.Name)
                {
                    Description = funcDescription,
                    Parameters = new BinaryData(json)
                });
            }
        }

        public IEnumerable<FunctionDefinition> GetFunctionDefinitions()
        {
            return _functions.Values;
        }

        public bool TryCallFunction(ChatCompletionsOptions options, HelperFunctionCallContext context, out string? result)
        {
            result = null;
            if (!string.IsNullOrEmpty(context.FunctionName) && !string.IsNullOrEmpty(context.Arguments))
            {
                var function = _functions.FirstOrDefault(x => x.Value.Name == context.FunctionName);
                if (function.Key != null)
                {
                    result = CallFunction(function.Key, function.Value, context.Arguments);
                    options.Messages.Add(new ChatMessage() { Role = ChatRole.Assistant, FunctionCall = new FunctionCall(context.FunctionName, context.Arguments) });
                    options.Messages.Add(new ChatMessage(ChatRole.Function, result) { Name = context.FunctionName });
                    return true;
                }
            }
            return false;
        }

        // operator to add to FunctionFactories together
        public static HelperFunctionFactory operator +(HelperFunctionFactory a, HelperFunctionFactory b)
        {
            var newFactory = new HelperFunctionFactory();
            a._functions.ToList().ForEach(x => newFactory._functions.Add(x.Key, x.Value));
            b._functions.ToList().ForEach(x => newFactory._functions.Add(x.Key, x.Value));
            return newFactory;
        }

        private static string? CallFunction(MethodInfo methodInfo, FunctionDefinition functionDefinition, string argumentsAsJson)
        {
            var jObject = JObject.Parse(argumentsAsJson);
            var arguments = new List<object>();

            var schema = functionDefinition.Parameters.ToString();
            var schemaObject = JObject.Parse(schema);

            var parameters = methodInfo.GetParameters();
            foreach (var parameter in parameters)
            {
                var parameterName = parameter.Name;
                if (parameterName == null) continue;

                var parameterType = parameter.ParameterType.Name;
                var parameterValue = jObject[parameterName]?.ToString();

                if (parameterType == "String" && parameterValue != null)
                {
                    arguments.Add(parameterValue);
                }
                else if (parameterType == "Int32" && parameterValue != null)
                {
                    arguments.Add(int.Parse(parameterValue));
                }
                else
                {
                    var isRequired = schemaObject["required"]?.Values<string>().Contains(parameterName) ?? false;
                    if (isRequired) throw new Exception($"Unknown parameter type: {parameterType}");
                }
            }

            var result = methodInfo.Invoke(null, arguments.ToArray());
            return result?.ToString();
        }

        private static string GetMethodParametersJsonSchema(MethodInfo method, ref object[] attributes)
        {
            var required = new JArray();
            var parameters = new JObject();
            parameters["type"] = "object";

            var properties = new JObject();
            parameters["properties"] = properties;

            foreach (var parameter in method.GetParameters())
            {
                var parameterName = parameter.Name;
                if (parameterName == null) continue;

                var parameterType = parameter.ParameterType.Name switch
                {
                    "String" => "string",
                    "Int32" => "integer",
                    _ => parameter.ParameterType.Name,
                };

                attributes = parameter.GetCustomAttributes(typeof(HelperFunctionParameterDescriptionAttribute), false);
                var paramDescriptionAttrib = attributes.Length > 0 ? (attributes[0] as HelperFunctionParameterDescriptionAttribute) : null;
                var paramDescription = paramDescriptionAttrib?.Description ?? $"The {parameterName} parameter";

                var parameterJson = new JObject();
                parameterJson["type"] = parameterType;
                parameterJson["description"] = paramDescription;
                properties[parameterName] = parameterJson;

                if (!parameter.IsOptional)
                {
                    required.Add(parameterName);
                }
            }

            parameters["required"] = required;

            var json = parameters.ToString(Formatting.None);
            return json;
        }

        private Dictionary<MethodInfo, FunctionDefinition> _functions = new();
    }
}
