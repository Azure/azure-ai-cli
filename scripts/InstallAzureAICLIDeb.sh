#!/bin/bash

# Check to see if we need to have the user specify the version of Azure.AI.CLI they want to install
AICLI_VERSION="PLACEHOLDER_VERSION"
if [ "$AICLI_VERSION" == "PLACEHOLDER_VERSION" ]; then
    # if arg1 is set, use it as the version
    if [ ! -z "$1" ]; then
        AICLI_VERSION="$1"
    fi
    if [ "$AICLI_VERSION" == "PLACEHOLDER_VERSION" ]; then
        echo "Please specify the version of Azure.AI.CLI you want to install."
        echo "Usage: $0 <version>"
        exit 1
    fi
fi

# Set the Azure.AI.CLI nuget package filename
AICLI_NUPKG_FILENAME="Azure.AI.CLI.${AICLI_VERSION}.nupkg"

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    # Install Azure CLI
    echo "Installing Azure CLI..."
    curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

    # Check if the installation was successful
    if [ $? -ne 0 ]; then
        echo "Failed to install Azure CLI."
        exit 1
    else
        echo "Installing Azure CLI... Done!"
    fi
fi

# Check if dotnet 7.0 is installed
if ! command -v dotnet &> /dev/null; then
    # Update the package list
    sudo apt-get update

    # Check the distribution (Ubuntu or Debian) AND version using /etc/os-release
    CHECK_DISTRO=$(awk -F= '/^ID=/{print $2}' /etc/os-release)
    CHECK_VERSION=$(grep -oP '(?<=VERSION_ID=").*(?=")' /etc/os-release)

    if [[ "$CHECK_DISTRO" == "ubuntu" ]]; then
        if [[ "$CHECK_VERSION" == "20.04" ]]; then
            # Install the Microsoft package signing key for Ubuntu 20.04
            wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            sudo dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
        elif [[ "$CHECK_VERSION" == "22.04" ]]; then
            # We don't need to install the Microsoft package signing key for Ubuntu 22.04; in fact, if we do, `dotnet tool` doesn't work
            echo "Ubuntu 22.04 detected. Skipping Microsoft package signing key installation."
            # wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            # sudo dpkg -i packages-microsoft-prod.deb
            # rm packages-microsoft-prod.deb
        else
            echo "Unsupported Ubuntu version: $CHECK_VERSION"
            exit 1
        fi
    elif [[ "$CHECK_DISTRO" == "debian" ]]; then
        if [[ "$CHECK_VERSION" == "10" ]]; then
            # Install the Microsoft package signing key for Debian 10
            wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            sudo dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
        elif [[ "$CHECK_VERSION" == "11" ]]; then
            # Install the Microsoft package signing key for Debian 11
            wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            sudo dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
        elif [[ "$CHECK_VERSION" == "12" ]]; then
            # Install the Microsoft package signing key for Debian 12
            wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            sudo dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
        else
            # Unsupported Debian version
            echo "Unsupported Debian version: $CHECK_VERSION"
            exit 1
        fi
    else
        # Unsupported distribution
        echo "Unsupported distribution: $CHECK_DISTRO"
        exit 1
    fi
    
    # Install dotnet 7.0 runtime
    sudo apt-get update
    sudo apt-get install -y dotnet-sdk-7.0
    
    # Check if the installation was successful
    if [ $? -ne 0 ]; then
        echo "Failed to install Dotnet 7.0."
        exit 1
    fi
fi

# Download the Azure.AI.CLI nuget package
echo "Downloading Azure.AI.CLI package..."
wget "https://csspeechstorage.blob.core.windows.net/drop/private/ai/${AICLI_NUPKG_FILENAME}" -O "${AICLI_NUPKG_FILENAME}"

# Check if the download was successful
if [ $? -ne 0 ]; then
    echo "Failed to download Azure.AI.CLI package."
    exit 1
else
    echo "Downloading Azure.AI.CLI package... Done!"
fi

# Install the Azure.AI.CLI dotnet tool
echo "Installing Azure.AI.CLI..."

if [ "$EUID" -ne 0 ]; then # if we're not root
    dotnet tool install --global --add-source . Azure.AI.CLI --version ${AICLI_VERSION}
    DOTNET_TOOLS_PATH="$HOME/.dotnet/tools"
elif [ -n "$SUDO_USER" ]; then # if we're root and SUDO_USER is set, run as SUDO_USER
    sudo -u $SUDO_USER dotnet tool install --global --add-source . Azure.AI.CLI --version ${AICLI_VERSION}
    DOTNET_TOOLS_PATH="/home/$SUDO_USER/.dotnet/tools"
else # if we're root and SUDO_USER is not set, we can't proceed
    echo "Cannot determine the user to install the Azure.AI.CLI dotnet tool for."
    exit 1
fi

# Check if the installation was successful
if [ $? -ne 0 ]; then
    echo "Failed to install Azure.AI.CLI."
    exit 1
elif [ ! -d "$DOTNET_TOOLS_PATH" ]; then
    echo "Warning: $DOTNET_TOOLS_PATH directory not found."
    exit 1
else
    echo "Azure.AI.CLI has been successfully installed at $DOTNET_TOOLS_PATH"
    rm "${AICLI_NUPKG_FILENAME}"
fi

# Add the .NET tools directory to the PATH
echo ""
echo "Adding $DOTNET_TOOLS_PATH to PATH..."
export PATH="$DOTNET_TOOLS_PATH:$PATH"                               # For current shell
echo "export PATH=\"$DOTNET_TOOLS_PATH:\$PATH\"" >> "$HOME/.bashrc"  # For bash
echo "export PATH=\"$DOTNET_TOOLS_PATH:\$PATH\"" >> "${ZDOTDIR:-$HOME}/.zshrc"   # For zsh (if using)
echo ""
echo "Don't forget to source your shell's rc file, for example:"
echo ""
echo "source ~/.bashrc"
echo ""

# Exit with appropriate status code
exit 0
