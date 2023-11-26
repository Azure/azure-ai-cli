using System.Reflection;
using Newtonsoft.Json;
using Azure.AI.OpenAI;
using Newtonsoft.Json.Linq;
using System.Collections;

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
                    System.Console.WriteLine($"\nFunction: {method.Name}");
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
            var jObject = JObject.Parse(argumentsAsJson);
            var arguments = new List<object>();

            var parameters = methodInfo.GetParameters();
            foreach (var parameter in parameters)
            {
                var parameterName = parameter.Name;
                if (parameterName == null) continue;

                var parameterValue = jObject[parameterName]?.ToString();
                if (string.IsNullOrEmpty(parameterValue)) continue;

                var parsed = ParseParameterValue(parameterValue, parameter.ParameterType);
                arguments.Add(parsed);
            }

            var args = arguments.ToArray();
            var result = methodInfo.Invoke(null, args);
            return result?.ToString();
        }

        private static object ParseParameterValue(string parameterValue, Type parameterType)
        {
            if (parameterType.IsArray)
            {
                Type elementType = parameterType.GetElementType()!;
                return CreateGenericCollectionFromJsonArray(parameterValue, typeof(Array), elementType);
            }

            if (IsGenericListOrEquivalent(parameterType))
            {
                Type elementType = parameterType.GetGenericArguments()[0];
                return CreateGenericCollectionFromJsonArray(parameterValue, typeof(List<>), elementType);
            }

            switch (Type.GetTypeCode(parameterType))
            {
                case TypeCode.Boolean: return bool.Parse(parameterValue!);
                case TypeCode.Byte: return byte.Parse(parameterValue!);
                case TypeCode.Decimal: return decimal.Parse(parameterValue!);
                case TypeCode.Double: return double.Parse(parameterValue!);
                case TypeCode.Single: return float.Parse(parameterValue!);
                case TypeCode.Int16: return short.Parse(parameterValue!);
                case TypeCode.Int32: return int.Parse(parameterValue!);
                case TypeCode.Int64: return long.Parse(parameterValue!);
                case TypeCode.SByte: return sbyte.Parse(parameterValue!);
                case TypeCode.UInt16: return ushort.Parse(parameterValue!);
                case TypeCode.UInt32: return uint.Parse(parameterValue!);
                case TypeCode.UInt64: return ulong.Parse(parameterValue!);
                case TypeCode.String: return parameterValue!;
                default: return Convert.ChangeType(parameterValue!, parameterType);
            }
        }

        private static object CreateGenericCollectionFromJsonArray(string parameterValue, Type collectionType, Type elementType)
        {
            var array = JArray.Parse(parameterValue);

            if (collectionType == typeof(Array))
            {
                var collection = Array.CreateInstance(elementType, array.Count);
                for (int i = 0; i < array.Count; i++)
                {
                    var parsed = ParseParameterValue(array[i].ToString(), elementType);
                    if (parsed != null) collection.SetValue(parsed, i);
                }
                return collection;
            }
            else if (collectionType == typeof(List<>))
            {
                var collection = Activator.CreateInstance(collectionType.MakeGenericType(elementType));
                var list = collection as IList;
                foreach (var item in array)
                {
                    var parsed = ParseParameterValue(item.ToString(), elementType);
                    if (parsed != null) list!.Add(parsed);
                }
                return collection!;
            }

            return array;
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
            return IsGenericListOrEquivalent(parameter.ParameterType) || parameter.ParameterType.IsArray
                ? GetArrayOrGenericListEquivalentJsonSchema(parameter)
                : GetJsonSchemaForPrimative(parameter);
        }

        private static bool IsGenericListOrEquivalent(Type t)
        {
            return t.IsGenericType &&
               (t.GetGenericTypeDefinition() == typeof(List<>) ||
                t.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                t.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                t.GetGenericTypeDefinition() == typeof(IList<>) ||
                t.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>) ||
                t.GetGenericTypeDefinition() == typeof(IReadOnlyList<>));
        }

        private static JToken GetArrayOrGenericListEquivalentJsonSchema(ParameterInfo parameter)
        {
            var parameterJson = new JObject();
            parameterJson["type"] = "array";

            var itemType = GetJsonTypeForArrayOrGenericListEquivalent(parameter);
            parameterJson["items"] = new JObject() { ["type"] = itemType };

            parameterJson["description"] = GetParameterDescription(parameter);
            return parameterJson;
        }

        private static string GetJsonTypeForArrayOrGenericListEquivalent(ParameterInfo parameter)
        {
            return parameter.ParameterType.IsArray
                ? GetJsonTypeFromPrimitive(parameter.ParameterType.GetElementType()!)
                : GetJsonTypeFromPrimitive(parameter.ParameterType.GetGenericArguments()[0]);
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
