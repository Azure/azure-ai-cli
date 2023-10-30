//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class BlobContainerToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser(bool blobPartRequired = true) => new NamedValueTokenParser(_optionName, _fullName, blobPartRequired ? "11" : "01", "1");

        private const string _requiredDisplayName = "blob container";
        private const string _optionName = "--blob-container";
        private const string _optionExample = "https://ACCOUNT_NAME.blob.core.windows.net/CONTAINER_NAME";
        private const string _fullName = "blob.container";
    }
}
