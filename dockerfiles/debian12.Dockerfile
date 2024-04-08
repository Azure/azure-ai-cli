# BASE layer -----------------------------------------------

# Use the base image for Debian 12 (bookworm)
FROM mcr.microsoft.com/devcontainers/python:3.12-bookworm AS base
ENV DEBIAN_FRONTEND=noninteractive

# Install dependencies
WORKDIR /

# Install dependencies
RUN apt-get update
RUN apt install fuse -y
RUN apt install dos2unix -y
COPY requirements.txt .
RUN pip install -r requirements.txt --break-system-packages

# Copy the required scripts into the container
WORKDIR /_scratch
COPY ./scripts/InstallAzureAICLIDeb.sh /_scratch/
COPY ./scripts/InstallAzureAICLIDeb-UpdateVersion.sh /_scratch/

# Feature flags/arguments
ARG AZURE_CLI_VERSION=1.0.0-alpha1107.1
ARG DOWNLOAD_SCRIPT=false

# If we're downloading the script, do so
RUN if [ "${DOWNLOAD_SCRIPT}" = "true" ]; then \
    wget https://csspeechstorage.blob.core.windows.net/drop/private/ai/InstallAzureAICLIDeb-${AZURE_CLI_VERSION}.sh -O /_scratch/InstallAzureAICLIDeb-${AZURE_CLI_VERSION}.sh; \
    fi

# If we're not downloading the script, update the version
RUN if [ "${DOWNLOAD_SCRIPT}" = "false" ]; then \
    chmod +x InstallAzureAICLIDeb-UpdateVersion.sh && \
    /bin/bash InstallAzureAICLIDeb-UpdateVersion.sh ${AZURE_CLI_VERSION} /_scratch/ && \
    dos2unix /_scratch/InstallAzureAICLIDeb-${AZURE_CLI_VERSION}.sh; \
    fi

# Copy installation script into the container, and make sure it is executable
RUN chmod +x InstallAzureAICLIDeb-${AZURE_CLI_VERSION}.sh

# Install Azure AI CLI as a non-root user
USER vscode
RUN sudo chown -R vscode /_scratch && \
    /bin/bash InstallAzureAICLIDeb-${AZURE_CLI_VERSION}.sh && \
    rm ./InstallAzureAICLIDeb-${AZURE_CLI_VERSION}.sh

# TEST layer -----------------------------------------------
FROM base AS test
USER vscode

# Copy test script into the container
WORKDIR /_scratch
COPY ./scripts/InstallAzureAICLI-test.sh /_scratch/
RUN sudo chmod +x InstallAzureAICLI-test.sh

# Run tests 
RUN /bin/bash InstallAzureAICLI-test.sh
RUN rm ./InstallAzureAICLI-test.sh
RUN /home/vscode/.dotnet/tools/ai config . @passed --set true

# FINAL layer ----------------------------------------------
FROM base AS final
USER vscode

# Copy the test results into the final image
WORKDIR /home/vscode
COPY --from=test /_scratch/passed /_scratch/passed

# Ensure the test passed
RUN test -f /_scratch/passed && \
    sudo rm -r /_scratch

# Define the entry point for your tool
ENTRYPOINT ["/home/vscode/.dotnet/tools/ai"]
CMD ["--help"]
