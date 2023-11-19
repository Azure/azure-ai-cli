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

    public class AzureOpenAIFunctionDefinitionHelper
    {
        public static Dictionary<MethodInfo, FunctionDefinition> GetFunctionDefinitions(Type type)
        {
            var functions = new Dictionary<MethodInfo, FunctionDefinition>();

            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (var method in methods)
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

                    functions.Add(method, functionDefinition);
                }
            }

            return functions;
        }

        public static Dictionary<MethodInfo, FunctionDefinition> GetFunctionDefinitions(IEnumerable<Type> types)
        {
            var functions = new Dictionary<MethodInfo, FunctionDefinition>();
            foreach (var type in types)
            {
                var typeFunctions = GetFunctionDefinitions(type);
                foreach (var typeFunction in typeFunctions)
                {
                    functions.Add(typeFunction.Key, typeFunction.Value);
                }
            }
            return functions;
        }

        public static Dictionary<MethodInfo, FunctionDefinition> GetFunctionDefinitions(Type type1, params Type[] types)
        {
            var functions = GetFunctionDefinitions(type1);
            foreach (var type in types)
            {
                var typeFunctions = GetFunctionDefinitions(type);
                foreach (var typeFunction in typeFunctions)
                {
                    functions.Add(typeFunction.Key, typeFunction.Value);
                }
            }
            return functions;
        }

        public static Dictionary<MethodInfo, FunctionDefinition> GetFunctionDefinitions(Assembly assembly)
        {
            return GetFunctionDefinitions(assembly.GetTypes());
        }

        public static string CallFunction(MethodInfo methodInfo, FunctionDefinition functionDefinition, string argumentsAsJson)
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
    }
}
