#!/bin/bash

# Check if dotnet 7.0 is installed
if ! command -v dotnet &> /dev/null; then

    # Ask the user if they want to install dotnet 7.0 (default is Yes)
    read -p "Dotnet 7.0 is not installed. Install? [Y/n] " -n 1 -r
    if [[ $REPLY =~ ^[Nn]$ ]]; then
        exit 1
    fi

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
            # Install the Microsoft package signing key for Ubuntu 22.04
            wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            sudo dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
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
wget https://csspeechstorage.blob.core.windows.net/drop/private/ai/Azure.AI.CLI.1.0.0-alpha11.nupkg -O Azure.AI.CLI.1.0.0-alpha11.nupkg

# Check if the download was successful
if [ $? -ne 0 ]; then
    echo "Failed to download Azure.AI.CLI package."
    exit 1
fi

# Install the Azure.AI.CLI dotnet tool
echo "Installing Azure.AI.CLI..."
dotnet tool install --global --add-source . Azure.AI.CLI --version 1.0.0-alpha11

# Check if the installation was successful
if [ $? -eq 0 ]; then
    echo "Azure.AI.CLI has been successfully installed."

    # Add the .NET tools directory to the PATH
    DOTNET_TOOLS_PATH="$HOME/.dotnet/tools"
    if [ -d "$DOTNET_TOOLS_PATH" ]; then
        echo "Adding $DOTNET_TOOLS_PATH to PATH..."
        echo "export PATH=\"$DOTNET_TOOLS_PATH:\$PATH\"" >> "$HOME/.bashrc"  # For bash
        echo "export PATH=\"$DOTNET_TOOLS_PATH:\$PATH\"" >> "$HOME/.zshrc"   # For zsh (if using)
        echo "Don't forget to source your shell's rc file, for example:"
        echo ""
        echo "source ~/.bashrc"
        echo ""
    else
        echo "Warning: $DOTNET_TOOLS_PATH directory not found."
        exit 1
    fi
else
    echo "Failed to install Azure.AI.CLI."
    exit 1
fi

# Clean up - remove the downloaded package
rm Azure.AI.CLI.1.0.0-alpha11.nupkg

# Exit with appropriate status code
exit 0
