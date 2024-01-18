//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class ProgrammingLanguageToken
    {
        public static string GetExtension(string language)
        {
            return language?.ToLower() switch
            {
                "c#" => ".cs",
                "go" => ".go",
                "java" => ".java",
                "javascript" => ".js",
                "python" => ".py",
                "typescript" => ".ts",
                _ => string.Empty
            };
        }

        public static string GetSuffix(string language)
        {
            return GetExtension(language).Replace(".", "-");
        }

        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => new NamedValueTokenParserList(
            new NamedValueTokenParser(_optionName, _fullName, "01", "1", "C#;c#;cs;Go;go;Java;java;JavaScript;javascript;js;Python;python;py;TypeScript;typescript;ts"),
            new NamedValueTokenParser("--C#", "programming.language.csharp", "001", "0", null, null, "C#", _fullName),
            new NamedValueTokenParser("--CS", "programming.language.csharp", "001", "0", null, null, "C#", _fullName),
            new NamedValueTokenParser("--Go", "programming.language.go", "001", "0", null, null, "Go", _fullName),
            new NamedValueTokenParser("--Java", "programming.language.java", "001", "0", null, null, "Java", _fullName),
            new NamedValueTokenParser("--JavaScript", "programming.language.javascript", "001", "0", null, null, "JavaScript", _fullName),
            new NamedValueTokenParser("--JS", "programming.language.javascript", "001", "0", null, null, "JavaScript", _fullName),
            new NamedValueTokenParser("--Python", "programming.language.python", "001", "0", null, null, "Python", _fullName),
            new NamedValueTokenParser("--PY", "programming.language.python", "001", "0", null, null, "Python", _fullName),
            new NamedValueTokenParser("--TypeScript", "programming.language.typescript", "001", "0", null, null, "TypeScript", _fullName),
            new NamedValueTokenParser("--TS", "programming.language.typescript", "001", "0", null, null, "TypeScript", _fullName)
        );

        private const string _requiredDisplayName = "programming language";
        private const string _optionName = "--language";
        private const string _optionExample = "LANGUAGE";
        private const string _fullName = "programming.language";
    }
}
