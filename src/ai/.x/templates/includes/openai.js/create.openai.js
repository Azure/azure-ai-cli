{{set _USE_OPENAI_CLOUD_AZURE=false}}
{{set _USE_OPENAI_CLOUD_OPENAI=false}}
{{set _USE_OPENAI_CLOUD_EITHER=false}}
{{set _USE_AZURE_OPENAI_WITH_KEY=false}}
{{if contains(toupper("{OPENAI_CLOUD}"), "AZURE")}}
  {{set _USE_OPENAI_CLOUD_AZURE=true}}
  {{if contains(toupper("{AZURE_OPENAI_AUTH_METHOD}"), "KEY")}}
    {{set _USE_AZURE_OPENAI_WITH_KEY=true}}
  {{endif}}
{{endif}}
{{if contains(toupper("{OPENAI_CLOUD}"), "OPENAI")}}
  {{set _USE_OPENAI_CLOUD_OPENAI=true}}
{{endif}}
{{if {_USE_OPENAI_CLOUD_AZURE} && {_USE_OPENAI_CLOUD_OPENAI}}}
  {{set _USE_OPENAI_CLOUD_EITHER=true}}
{{endif}}
{{if !{_IS_LEARN_DOC_TEMPLATE}}}
  // What's the system prompt?
  const AZURE_OPENAI_SYSTEM_PROMPT = process.env.AZURE_OPENAI_SYSTEM_PROMPT ?? "You are a helpful AI assistant.";

  {{if {_USE_OPENAI_CLOUD_OPENAI} || {_USE_AZURE_OPENAI_WITH_KEY}}}
  // NOTE: Never deploy your API Key in client-side environments like browsers or mobile apps
  // SEE: https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety
  {{endif}}
{{endif}}

{{@include openai.js/environment.vars.js}}

{{if {_IS_AUTHOR_COMMENT} }}
// For some Learn docs and snippets, error handling or additional helpers are excluded to keep
// the code scoped and focused on the main concept being showcased.
{{endif}}
{{if !{_IS_LEARN_DOC_TEMPLATE}}}

{{if {_USE_OPENAI_CLOUD_AZURE}}}
  {{if {_USE_OPENAI_CLOUD_OPENAI}}}
    {{if {_IS_LEARN_DOC_TEMPLATE}}}
  const client = new AzureOpenAI({ endpoint, apiKey, apiVersion, deployment });
  const result = await client.chat.completions.create({
    messages: [
    { role: "system", content: "You are a helpful assistant." },
    { role: "user", content: "Does Azure OpenAI support customer managed keys?" },
    { role: "assistant", content: "Yes, customer managed keys are supported by Azure OpenAI?" },
    { role: "user", content: "Do other Azure AI services support this too?" },
    ],
    model: "",
  });
    {{else}}
  // Create the AzureOpenAI client
  console.log(azureOk
    ? 'Using Azure OpenAI (w/ API Key)...'
    : 'Using OpenAI...');
  const client = !azureOk
    ? new AzureOpenAI({
        apiKey: OPENAI_API_KEY,
        {{if {_IS_BROWSER_TEMPLATE}}}
        dangerouslyAllowBrowser: true
        {{endif}}
      })
    : new AzureOpenAI({
        apiKey: AZURE_OPENAI_API_KEY,
        baseURL: AZURE_OPENAI_BASE_URL,
        defaultQuery: { 'api-version': AZURE_OPENAI_API_VERSION },
        defaultHeaders: { 'api-key': AZURE_OPENAI_API_KEY },
        {{if {_IS_BROWSER_TEMPLATE}}}
        dangerouslyAllowBrowser: true
        {{endif}}
      });
    {{endif}}
  {{else if {_USE_AZURE_OPENAI_WITH_KEY}}}
  // Create the AzureOpenAI client
  console.log('Using Azure OpenAI (w/ API Key)...');
  const client = new AzureOpenAI({
    apiKey: AZURE_OPENAI_API_KEY,
    baseURL: AZURE_OPENAI_BASE_URL,
    defaultQuery: { 'api-version': AZURE_OPENAI_API_VERSION },
    defaultHeaders: { 'api-key': AZURE_OPENAI_API_KEY },
    {{if {_IS_BROWSER_TEMPLATE}}}
    dangerouslyAllowBrowser: true
    {{endif}}
  });
  {{else}}
  // Get the access token using the DefaultAzureCredential
  let token = null;
  try {
    {{if {_IS_BROWSER_TEMPLATE}}}
    const { InteractiveBrowserCredential } = require('@azure/identity');
    const credential = new InteractiveBrowserCredential({
      clientId: AZURE_CLIENT_ID,
      tenantId: AZURE_TENANT_ID,
      loginStyle: 'redirect' });
    {{else}}
    const { DefaultAzureCredential } = require('@azure/identity');
    const credential = new DefaultAzureCredential();
    {{endif}}
    const response = await credential.getToken("https://cognitiveservices.azure.com/.default");
    token = response.token;
  } catch (error) {  
    console.error('Error getting access token:', error);  
    throw error;  
  }

  // Create the AzureOpenAI client
  console.log('Using Azure OpenAI (w/ AAD)...');
  const client = new AzureOpenAI({
    apiKey: '',
    baseURL: AZURE_OPENAI_BASE_URL,
    defaultQuery: { 'api-version': AZURE_OPENAI_API_VERSION },
    defaultHeaders: { Authorization: `Bearer ${token}` },
    {{if {_IS_BROWSER_TEMPLATE}}}
    dangerouslyAllowBrowser: true
    {{endif}}
  });
  {{endif}}
{{else}}
  // Create the OpenAI client
  console.log('Using OpenAI...');
  const openai = new OpenAI({
    apiKey: OPENAI_API_KEY,
    organization: OPENAI_ORG_ID,
    {{if {_IS_BROWSER_TEMPLATE}}}
    dangerouslyAllowBrowser: true
    {{endif}}
  });
{{endif}}

{{if {_IS_AUTHOR_COMMENT} }}
// For some Learn docs and snippets, error handling or additional helpers are excluded to keep
// the code scoped and focused on the main concept being showcased.
{{endif}}
{{if !{_IS_LEARN_DOC_TEMPLATE}}}
  // Create the streaming chat completions helper
  {{if contains(toupper("{OPENAI_CLOUD}"), "AZURE")}}
  const chat = new {ClassName}(AZURE_OPENAI_CHAT_DEPLOYMENT, AZURE_OPENAI_SYSTEM_PROMPT, client);
  {{else}}
  const chat = new {ClassName}(OPENAI_MODEL_NAME, AZURE_OPENAI_SYSTEM_PROMPT, openai);
  {{endif}}
{{endif}}