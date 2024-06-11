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
{{if {_USE_OPENAI_CLOUD_OPENAI} || {_USE_AZURE_OPENAI_WITH_KEY}}}
  // NOTE: Never deploy your API Key in client-side environments like browsers or mobile apps
  // SEE: https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety

{{endif}}

{{@include openai.js/environment.vars.js}}

{{if {_USE_OPENAI_CLOUD_AZURE}}}
  {{if {_USE_OPENAI_CLOUD_OPENAI}}}
  // Create the OpenAI client
  console.log(azureOk
    ? 'Using Azure OpenAI (w/ API Key)...'
    : 'Using OpenAI...');
  const openai = !azureOk
    ? new OpenAI({
        apiKey: OPENAI_API_KEY,
        {{if {_IS_BROWSER_TEMPLATE}}}
        dangerouslyAllowBrowser: true
        {{endif}}
      })
    : new OpenAI({
        apiKey: AZURE_OPENAI_API_KEY,
        baseURL: AZURE_OPENAI_BASE_URL,
        defaultQuery: { 'api-version': AZURE_OPENAI_API_VERSION },
        defaultHeaders: { 'api-key': AZURE_OPENAI_API_KEY },
        {{if {_IS_BROWSER_TEMPLATE}}}
        dangerouslyAllowBrowser: true
        {{endif}}
      });
  {{else if {_USE_AZURE_OPENAI_WITH_KEY}}}
  // Create the OpenAI client
  console.log('Using Azure OpenAI (w/ API Key)...');
  const openai = new OpenAI({
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

  // Create the OpenAI client
  console.log('Using Azure OpenAI (w/ AAD)...');
  const openai = new OpenAI({
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