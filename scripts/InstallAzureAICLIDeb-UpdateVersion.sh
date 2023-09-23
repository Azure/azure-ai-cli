#!/bin/bash

# print usage if parameter 1 is not set
if [ -z "$1" ]; then
    echo "Usage: $0 <version> [output-path]"
    exit 1
else
    AICLI_VERSION="$1"
fi

# directory of this script
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

# if parameter 2 is set, use it as the output path
if [ -z "$2" ]; then
    OUTPUT_PATH="${DIR}"
else
    OUTPUT_PATH="$2"
fi

# setup the source and destination filenames
AICLI_INSTALL_SCRIPT_FILENAME_NO_VERSION="${DIR}/InstallAzureAICLIDeb.sh"
AICLI_INSTALL_SCRIPT_FILENAME_WITH_VERSION="${OUTPUT_PATH}/InstallAzureAICLIDeb-${AICLI_VERSION}.sh"

# update the version in the install script
cp ${AICLI_INSTALL_SCRIPT_FILENAME_NO_VERSION} ${AICLI_INSTALL_SCRIPT_FILENAME_WITH_VERSION}
sed -i "s/AICLI_VERSION=\"PLACEHOLDER_VERSION\"/AICLI_VERSION=\"${AICLI_VERSION}\"/" ${AICLI_INSTALL_SCRIPT_FILENAME_WITH_VERSION}
