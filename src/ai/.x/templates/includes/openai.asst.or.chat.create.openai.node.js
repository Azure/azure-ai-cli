{{if {USE_AZURE_OPENAI}}}
  {{if {_IS_OPENAI_ASST_TEMPLATE}}}
  // Get the required environment variables, and form the base URL for Azure OpenAI Assistants API
  {{else}}
  // Get the required environment variables, and form the base URL for Azure OpenAI Chat Completions API
  {{endif}}
  {{if contains(toupper("{AZURE_OPENAI_AUTH_METHOD}"), "KEY")}}
  const AZURE_OPENAI_API_KEY = process.env.AZURE_OPENAI_API_KEY ?? "<insert your Azure OpenAI API key here>";
  {{else if {_IS_BROWSER_TEMPLATE}}}
  const AZURE_CLIENT_ID = process.env.AZURE_CLIENT_ID ?? null;
  const AZURE_TENANT_ID = process.env.AZURE_TENANT_ID ?? null;
  {{endif}}
  const AZURE_OPENAI_API_VERSION = process.env.AZURE_OPENAI_API_VERSION ?? "<insert your Azure OpenAI API version here>";
  const AZURE_OPENAI_ENDPOINT = process.env.AZURE_OPENAI_ENDPOINT ?? "<insert your Azure OpenAI endpoint here>";
  {{if {_IS_OPENAI_ASST_TEMPLATE}}}
  const AZURE_OPENAI_BASE_URL = `${AZURE_OPENAI_ENDPOINT.replace(/\/+$/, '')}/openai`;
  {{else}}
  const AZURE_OPENAI_CHAT_DEPLOYMENT = process.env.AZURE_OPENAI_CHAT_DEPLOYMENT ?? "<insert your Azure OpenAI chat deployment name here>";
  const AZURE_OPENAI_BASE_URL = `${AZURE_OPENAI_ENDPOINT.replace(/\/+$/, '')}/openai/deployments/${AZURE_OPENAI_CHAT_DEPLOYMENT}`;
  {{endif}}

  {{if contains(toupper("{AZURE_OPENAI_AUTH_METHOD}"), "KEY")}}
  // NOTE: Never deploy your key in client-side environments like browsers or mobile apps
  // SEE: https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety

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
  // Get the required environment variables, and form the base URL for OpenAI Platform API
  const OPENAI_API_KEY = process.env.OPENAI_API_KEY ?? "<insert your OpenAI API key here>";
  const OPENAI_MODEL_NAME = process.env.OPENAI_MODEL_NAME ?? "<insert your OpenAI model name here>";
  const OPENAI_ORG_ID = process.env.OPENAI_ORG_ID ?? null;

  // NOTE: Never deploy your key in client-side environments like browsers or mobile apps
  // SEE: https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety

  // Create the OpenAI client
  console.log('Using OpenAI...');
  const openai =  new OpenAI({
    apiKey: OPENAI_API_KEY,
    organization: OPENAI_ORG_ID,
    {{if {_IS_BROWSER_TEMPLATE}}}
    dangerouslyAllowBrowser: true
    {{endif}}
  });
{{endif}}