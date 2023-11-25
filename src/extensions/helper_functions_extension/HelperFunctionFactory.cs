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

                string json = GetMethodParametersJsonSchema(method);
                if (Program.Debug)
                {
                    System.Console.WriteLine($"Function: {method.Name}");
                    System.Console.WriteLine($"Description: {funcDescription}");
                    System.Console.WriteLine($"Parameters: {json}");
                }
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
            // update later from here: https://chat.openai.com/share/99019aec-c51c-49f9-a1ab-3f5b1be13c43

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

                if (parameterType == null) continue;
                switch (parameterType)
                {
                    case "Boolean": arguments.Add(bool.Parse(parameterValue!)); break;
                    case "byte": arguments.Add(byte.Parse(parameterValue!)); break;
                    case "decimal": arguments.Add(decimal.Parse(parameterValue!)); break;
                    case "double": arguments.Add(double.Parse(parameterValue!)); break;
                    case "float": arguments.Add(float.Parse(parameterValue!)); break;
                    case "Single": arguments.Add(Single.Parse(parameterValue!)); break;
                    case "Int16": arguments.Add(Int16.Parse(parameterValue!)); break;
                    case "Int32": arguments.Add(Int32.Parse(parameterValue!)); break;
                    case "Int64": arguments.Add(Int64.Parse(parameterValue!)); break;
                    case "long": arguments.Add(long.Parse(parameterValue!)); break;
                    case "sbyte": arguments.Add(sbyte.Parse(parameterValue!)); break;
                    case "short": arguments.Add(short.Parse(parameterValue!)); break;
                    case "String": arguments.Add(parameterValue!); break;
                    case "UInt16": arguments.Add(UInt16.Parse(parameterValue!)); break;
                    case "UInt32": arguments.Add(UInt32.Parse(parameterValue!)); break;
                    case "UInt64": arguments.Add(UInt64.Parse(parameterValue!)); break;
                    case "ulong": arguments.Add(ulong.Parse(parameterValue!)); break;
                    case "ushort": arguments.Add(ushort.Parse(parameterValue!)); break;
                    default: arguments.Add(parameterValue!); break;
                };
            }

            var result = methodInfo.Invoke(null, arguments.ToArray());
            return result?.ToString();
        }

        private static string GetMethodParametersJsonSchema(MethodInfo method)
        {
            var required = new JArray();
            var parameters = new JObject();
            parameters["type"] = "object";

            var properties = new JObject();
            parameters["properties"] = properties;

            foreach (var parameter in method.GetParameters())
            {
                if (parameter.Name == null) continue;

                properties[parameter.Name] = GetParameterJsonSchema(parameter);
                if (!parameter.IsOptional)
                {
                    required.Add(parameter.Name);
                }
            }

            parameters["required"] = required;

            return parameters.ToString(Formatting.None);
        }

        private static JToken GetParameterJsonSchema(ParameterInfo parameter)
        {
            return IsArrayParameter(parameter)
                ? GetArrayJsonSchema(parameter)
                : GetJsonSchemaForPrimative(parameter);
        }

        private static bool IsArrayParameter(ParameterInfo parameter)
        {
            return parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(List<>);
        }

        private static JToken GetArrayJsonSchema(ParameterInfo parameter)
        {
            var parameterJson = new JObject();
            parameterJson["type"] = "array";
            parameterJson["items"] = new JObject() { ["type"] = GetJsonTypeForGenericArgument(parameter, 0) };
            parameterJson["description"] = GetParameterDescription(parameter);
            return parameterJson;
        }

        private static string GetJsonTypeForGenericArgument(ParameterInfo parameter, int argumentIndex)
        {
            return GetJsonTypeFromPrimitive(parameter.ParameterType.GetGenericArguments()[argumentIndex]);
        }

        private static JToken GetJsonSchemaForPrimative(ParameterInfo parameter)
        {
            var parameterJson = new JObject();
            parameterJson["description"] = GetParameterDescription(parameter);
            parameterJson["type"] = GetJsonTypeFromPrimitive(parameter.ParameterType);
            return parameterJson;
        }

        private static string GetJsonTypeFromPrimitive(Type t)
        {
            return Type.GetTypeCode(t) switch
            {
                TypeCode.Boolean => "boolean",
                TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or
                TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => "integer",
                TypeCode.Decimal or TypeCode.Double or TypeCode.Single => "number",
                TypeCode.String => "string",
                _ => "string"
            };
        }

        private static string GetParameterDescription(ParameterInfo parameter)
        {
            var attributes = parameter.GetCustomAttributes(typeof(HelperFunctionParameterDescriptionAttribute), false);
            var paramDescriptionAttrib = attributes.Length > 0 ? (attributes[0] as HelperFunctionParameterDescriptionAttribute) : null;
            return  paramDescriptionAttrib?.Description ?? $"The {parameter.Name} parameter";
        }

        private Dictionary<MethodInfo, FunctionDefinition> _functions = new();
    }
}
