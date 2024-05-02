{{if !{USE_AZURE_OPENAI} || contains(toupper("{AZURE_OPENAI_AUTH_METHOD}"), "KEY")}}
  // NOTE: Never deploy your key in client-side environments like browsers or mobile apps
  // SEE: https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety

{{endif}}
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
  {{if !{_IS_OPENAI_ASST_TEMPLATE}}}
  const AZURE_OPENAI_CHAT_DEPLOYMENT = process.env.AZURE_OPENAI_CHAT_DEPLOYMENT ?? "<insert your Azure OpenAI chat deployment name here>";
  {{endif}}
  const AZURE_OPENAI_ENDPOINT = process.env.AZURE_OPENAI_ENDPOINT ?? "<insert your Azure OpenAI endpoint here>";
  {{if {_IS_OPENAI_ASST_TEMPLATE}}}
  const AZURE_OPENAI_BASE_URL = `${AZURE_OPENAI_ENDPOINT.replace(/\/+$/, '')}/openai`;
  {{else}}
  const AZURE_OPENAI_BASE_URL = `${AZURE_OPENAI_ENDPOINT.replace(/\/+$/, '')}/openai/deployments/${AZURE_OPENAI_CHAT_DEPLOYMENT}`;
  {{endif}}

  // Check if the required environment variables are set
  const ok = 
    {{if {_IS_OPENAI_ASST_TEMPLATE}}}
    ASSISTANT_ID != null && !ASSISTANT_ID.startsWith('<insert') &&
    {{else}}
    AZURE_OPENAI_SYSTEM_PROMPT != null && !AZURE_OPENAI_SYSTEM_PROMPT.startsWith('<insert') &&
    {{endif}}
    {{if contains(toupper("{AZURE_OPENAI_AUTH_METHOD}"), "KEY")}}
    AZURE_OPENAI_API_KEY != null && !AZURE_OPENAI_API_KEY.startsWith('<insert') &&
    {{else if {_IS_BROWSER_TEMPLATE}}}
    AZURE_CLIENT_ID != null && !AZURE_CLIENT_ID.startsWith('<insert') &&
    AZURE_TENANT_ID != null && !AZURE_TENANT_ID.startsWith('<insert') &&
    {{endif}}
    AZURE_OPENAI_API_VERSION != null && !AZURE_OPENAI_API_VERSION.startsWith('<insert') &&
    {{if !{_IS_OPENAI_ASST_TEMPLATE}}}
    AZURE_OPENAI_CHAT_DEPLOYMENT != null && !AZURE_OPENAI_CHAT_DEPLOYMENT.startsWith('<insert') &&
    {{endif}}
    AZURE_OPENAI_ENDPOINT != null && !AZURE_OPENAI_ENDPOINT.startsWith('<insert');

  if (!ok) {
    console.error('To use Azure OpenAI, set the following environment variables:\n' +
    {{if {_IS_OPENAI_ASST_TEMPLATE}}}
      '\n  ASSISTANT_ID' +
    {{else}}
      '\n  AZURE_OPENAI_SYSTEM_PROMPT' +
    {{endif}}
    {{if contains(toupper("{AZURE_OPENAI_AUTH_METHOD}"), "KEY")}}
      '\n  AZURE_OPENAI_API_KEY' +
    {{else}}
      {{if {_IS_BROWSER_TEMPLATE}}}
      '\n  AZURE_CLIENT_ID' +
      '\n  AZURE_TENANT_ID' +
      {{endif}}
    {{endif}}
      '\n  AZURE_OPENAI_API_VERSION' +
    {{if !{_IS_OPENAI_ASST_TEMPLATE}}}
      '\n  AZURE_OPENAI_CHAT_DEPLOYMENT' +
    {{endif}}
      '\n  AZURE_OPENAI_ENDPOINT');
    {{if {_IS_BROWSER_TEMPLATE}}}
    console.error('\nYou can easily do that using the Azure AI CLI by doing one of the following:\n' +
      '\n  ai init' +
      '\n  ai dev new .env' +
      '\n  npm run webpack' +
      '\n' +
      '\n  or' +
      '\n' +
      '\n  ai init' +
      '\n  ai dev shell' +
      '\n  npm run webpack' +
      '\n' +
      '\n  or' +
      '\n' +
      '\n  ai init' +
      '\n  ai dev shell --run "npm run webpack"');
    throw new Error('Missing required environment variables for Azure OpenAI');
    {{else}}
    console.error('\nYou can easily do that using the Azure AI CLI by doing one of the following:\n' +
      '\n  ai init' +
      '\n  ai dev shell' +
      '\n  node main.js' +
      '\n' +
      '\n  or' +
      '\n' +
      '\n  ai init' +
      '\n  ai dev shell --run "node main.js"');
    process.exit(1);
    {{endif}}
  }

  {{if contains(toupper("{AZURE_OPENAI_AUTH_METHOD}"), "KEY")}}
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
  {{if !{_IS_OPENAI_ASST_TEMPLATE}}}
  const OPENAI_MODEL_NAME = process.env.OPENAI_MODEL_NAME ?? "<insert your OpenAI model name here>";
  {{endif}}
  const OPENAI_ORG_ID = process.env.OPENAI_ORG_ID ?? null;

  // Check if the required environment variables are set
  const ok = 
    OPENAI_API_KEY != null && !OPENAI_API_KEY.startsWith('<insert');
    {{if {_IS_OPENAI_ASST_TEMPLATE}}}
    ASSISTANT_ID != null && !ASSISTANT_ID.startsWith('<insert');
    {{else}}
    OPENAI_MODEL_NAME != null && !OPENAI_MODEL_NAME.startsWith('<insert') &&
    AZURE_OPENAI_SYSTEM_PROMPT != null && !AZURE_OPENAI_SYSTEM_PROMPT.startsWith('<insert');
    {{endif}}

  if (!ok) {
    console.error('To use OpenAI, set the following environment variables:\n' +
      '\n  OPENAI_API_KEY' +
      '\n  OPENAI_ORG_ID (optional)' +
      {{if {_IS_OPENAI_ASST_TEMPLATE}}}
      '\n  ASSISTANT_ID');
      {{else}}
      '\n  OPENAI_MODEL_NAME' +
      '\n  AZURE_OPENAI_SYSTEM_PROMPT');
      {{endif}}
    console.error('\nYou can easily obtain some of these values by visiting these links:\n' +
      '\n  https://platform.openai.com/api-keys' +
      '\n  https://platform.openai.com/settings/organization/general' +
      '\n  https://platform.openai.com/playground/assistants' +
      '\n' +
    {{if {_IS_BROWSER_TEMPLATE}}}
      '\n Then, do one of the following:\n' +
      '\n  ai dev new .env' +
      '\n  npm run webpack' +
      '\n' +
      '\n  or' +
      '\n' +
      '\n  ai dev shell' +
      '\n  npm run webpack' +
      '\n' +
      '\n  or' +
      '\n' +
      '\n  ai dev shell --run "npm run webpack"');
      throw new Error('Missing required environment variables for OpenAI');
    {{else}}
      '\n Then, do one of the following:\n' +
      '\n  ai dev shell' +
      '\n  node main.js' +
      '\n' +
      '\n  or' +
      '\n' +
      '\n  ai dev shell --run "node main.js"');
      process.exit(1);
    {{endif}}
  }

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