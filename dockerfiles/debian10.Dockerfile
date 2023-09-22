# BASE layer -----------------------------------------------

# Use the base image for Debian 10 (buster)
FROM mcr.microsoft.com/devcontainers/base:buster AS base

# Copy installation script into the container, and make sure it is executable
WORKDIR /_scratch
COPY ./scripts/InstallAzureAICLIDeb-alpha11.sh /_scratch/
RUN chmod +x InstallAzureAICLIDeb-alpha11.sh

# Install Azure AI CLI as a non-root user
USER vscode
RUN sudo chown -R vscode /_scratch && \
    /bin/bash InstallAzureAICLIDeb-alpha11.sh && \
    rm ./InstallAzureAICLIDeb-alpha11.sh

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
