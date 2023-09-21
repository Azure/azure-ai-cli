#!/bin/bash

# Check the Ubuntu version
UBUNTU_VERSION=$(lsb_release -rs)

# Check if dotnet 7.0 is installed
if ! command -v dotnet &> /dev/null; then
    # Dotnet 7.0 is not installed, so we need to install it
    echo "Dotnet 7.0 is not installed. Installing..."
    
    if [[ $UBUNTU_VERSION == "20.04" ]]; then
        # Add Microsoft package repository for Ubuntu 20.04
        sudo apt-get update
        sudo apt-get install -y software-properties-common
        sudo apt-add-repository -y https://packages.microsoft.com/ubuntu/20.04/prod
    elif [[ $UBUNTU_VERSION == "22.04" ]]; then
        # Add Microsoft package repository for Ubuntu 22.04
        sudo apt-get update
        sudo apt-get install -y software-properties-common
        sudo apt-add-repository -y https://packages.microsoft.com/ubuntu/22.04/prod
    else
        # Unsupported Ubuntu version
        echo "Unsupported Ubuntu version: $UBUNTU_VERSION"
        exit 1
    fi
    
    # Install dotnet 7.0 runtime
    sudo apt-get update
    sudo apt-get install -y dotnet-sdk-7.0
    
    # Check if the installation was successful
    if [ $? -ne 0 ]; then
        echo "Failed to install Dotnet 7.0."
        exit 1
    else
        echo "Dotnet 7.0 has been successfully installed."
    fi
else
    # Dotnet 7.0 is already installed
    echo "Dotnet 7.0 is already installed."
fi

# Download the Azure.AI.CLI nuget package
echo "Downloading Azure.AI.CLI package..."
wget https://csspeechstorage.blob.core.windows.net/drop/private/ai/Azure.AI.CLI.1.0.0-alpha11.nupkg -O Azure.AI.CLI.1.0.0-alpha11.nupkg

# Check if the download was successful
if [ $? -ne 0 ]; then
    echo "Failed to download Azure.AI.CLI package."
    exit 1
else
    echo "Azure.AI.CLI package has been successfully downloaded."
fi

# Install the Azure.AI.CLI dotnet tool
echo "Installing Azure.AI.CLI..."
dotnet tool install --global --add-source . Azure.AI.CLI --version 1.0.0-alpha11

# Check if the installation was successful
if [ $? -eq 0 ]; then
    echo "Azure.AI.CLI has been successfully installed."
else
    echo "Failed to install Azure.AI.CLI."
fi

# Clean up - remove the downloaded package
rm Azure.AI.CLI.1.0.0-alpha11.nupkg

# Exit with appropriate status code
exit 0