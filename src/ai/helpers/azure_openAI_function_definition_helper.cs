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
        public static List<FunctionDefinition> GetFunctionDefinitions(Type type)
        {
            var functions = new List<FunctionDefinition>();

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

                    functions.Add(functionDefinition);
                }
            }


            return functions;
        }

        public static List<FunctionDefinition> GetFunctionDefinitions(IEnumerable<Type> types)
        {
            var functions = new List<FunctionDefinition>();
            foreach (var type in types)
            {
                functions.AddRange(GetFunctionDefinitions(type));
            }
            return functions;
        }

        public static List<FunctionDefinition> GetFunctionDefinitions(Type type1, params Type[] types)
        {
            var functions = GetFunctionDefinitions(type1);
            functions.AddRange(GetFunctionDefinitions(types));
            return functions;
        }

        public static List<FunctionDefinition> GetFunctionDefinitions(Assembly assembly)
        {
            return GetFunctionDefinitions(assembly.GetTypes());
        }
    }
}
