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
            new RequiredValidValueNamedValueTokenParser(_optionName, _fullName, "01", "C#;c#;cs;Go;go;Java;java;JavaScript;javascript;js;Python;python;py;TypeScript;typescript;ts"),
            new PinnedNamedValueTokenParser("--C#", "programming.language.csharp", "001", "C#", _fullName),
            new PinnedNamedValueTokenParser("--CS", "programming.language.csharp", "001", "C#", _fullName),
            new PinnedNamedValueTokenParser("--Go", "programming.language.go", "001", "Go", _fullName),
            new PinnedNamedValueTokenParser("--Java", "programming.language.java", "001", "Java", _fullName),
            new PinnedNamedValueTokenParser("--JavaScript", "programming.language.javascript", "001", "JavaScript", _fullName),
            new PinnedNamedValueTokenParser("--JS", "programming.language.javascript", "001", "JavaScript", _fullName),
            new PinnedNamedValueTokenParser("--Python", "programming.language.python", "001", "Python", _fullName),
            new PinnedNamedValueTokenParser("--PY", "programming.language.python", "001", "Python", _fullName),
            new PinnedNamedValueTokenParser("--TypeScript", "programming.language.typescript", "001", "TypeScript", _fullName),
            new PinnedNamedValueTokenParser("--TS", "programming.language.typescript", "001", "TypeScript", _fullName)
        );

        private const string _requiredDisplayName = "programming language";
        private const string _optionName = "--language";
        private const string _optionExample = "LANGUAGE";
        private const string _fullName = "programming.language";
    }
}
