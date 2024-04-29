# `ai` CLI Yaml Recording Test Adapter

In addition to the capabilities of the [Yaml Test Adapter](https://github.com/Azure/azure-ai-cli/tree/main/src/testing/adapters/testadapter), this adapter adds in the capability to record ai.exe <-> service traffic and replay it enabling CI/CD testing without service interaction.

The test yaml format and "how tos" are well covered in the Yaml Test Adapter pages, so you should become familiar with that as a first step.

## Marking a test as being recordable

Recordable tests are marked with a tag "Recordable" that should be set to true:
```
- name: test ai chat
  command: ai chat --question "Why is the sky blue, what's it called" --index-name @none
  expect-regex: Rayleigh
  tags: 
    recordable: true
```

The "Recordable" tag can be applied to a test, area, or class.

## Recording Tests

Test recording is done by setting the proxy the ai cli will use to a local nginx container that will route all traffic through the [Azure SDK's test-proxy](https://github.com/Azure/azure-sdk-tools/blob/main/tools/test-proxy/Azure.Sdk.Tools.TestProxy/README.md) and use it to record REST traffic.

### Setup

* [Install the Azure SDK Test Proxy](https://github.com/Azure/azure-sdk-tools/blob/main/tools/test-proxy/Azure.Sdk.Tools.TestProxy/README.md#installation)
```
dotnet tool update azure.sdk.tools.testproxy --global --add-source https://pkgs.dev.azure.com/azure-sdk/public/_packaging/azure-sdk-for-net/nuget/v3/index.json --version "1.0.0-dev*" --ignore-failed-sources
```
* Start the SDK Test Proxy
  * Point it's storage at your recordings directory.
  ```
  test-proxy -l \git\azure-ai-cli\tests\recordings
  ```

* [Run the script](../recordproxy/dev_insall.cmd) to build the local nginx proxy container.
This will:
  * Pull the base nginx container from the acrbn container registry.
  *  Build a local container and generate new TLS keys for itself.
  * Create and start a container instance with the correct port mappings. (5004:5004)
  * Install the containers Certificate Authority key in your local trusted root store. This is done because the proxy will need to intercept and decrypt the content before forwarding it on to the service.

* Start the proxy container
```
docker start nginx
```
* Set a TEST_MODE environment variable to "Recording"
```
set TEST_MODE=Record
```
If "TEST_MODE" is not set, the test cases will be run against the live services.

* If the AI feature you're testing will use the az cli, it needs an additional environment variable to accept the proxy certificates:
```
set REQUESTS_CA_BUNDLE=%TEMP%\ca.crt
```
(%TEMP%\ca.crt is where the dev_install.cmd script above left a copy of the proxy certificate.)

### Run the tests you want to record
```
dotnet test D:\git\azure-ai-cli\src\testing\adapters\recordingadapter\bin\Debug\net8.0\Azure.AI.CLI.RecordedTestAdapter.dll
```

### Inspect the results for secrets and other PII
Having recorded all client <-> service traffic means that some secrets, such as keys or tokens, or PII was likely recorded. You must inspect the .json files in the recordings directory to ensure that no inappropriate information is committed to the repository.

Once you've identified content that should not be committed, you can modify your test case to auto-sanitize the content.

To do so, add a _sanitize tag to your yaml:

```
- name: test ai chat
  command: ai chat --question "Why is the sky blue, what's it called" --index-name @none
  expect-regex: Rayleigh
  tags: 
    recordable: true
    _sanitize:
      - headers:
        - name: api-key
          value: 00000000-0000-0000-0000-000000000000
      - uri:
        - regex: https://(?<host>[^/]+)/
          value: https://fakeendpoint/
      - body:
        - regex: "\\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Z|a-z]{2,}\\b"
          value: "email@domain.com"
```
Supported sanitizers can search:
- The body of a request or response with a regular expression and replace matches with specified static content. 
- The URL of the request.
- A header with a given key and an optional regular expression for the value.

It is *important* to remember that the substituted values must retain any formatting, such as not breaking JSON syntax.

### Playback
Do the same setup as for recording, but instead of Record, set TEST_MODE to Playback.
```
set TEST_MODE=Record
```
### Commit the test case and the test\recordings directory.

Pretty self-explanatory. :-)