{{if !{_IS_LEARN_DOC_TEMPLATE}}}
// Get the required environment variables
{{if {_USE_OPENAI_CLOUD_AZURE}}}
  {{if {_USE_AZURE_OPENAI_WITH_KEY}}}
  const AZURE_OPENAI_API_KEY = {__process_env_or_import_meta_env}.AZURE_OPENAI_API_KEY ?? "<insert your Azure OpenAI API key here>";
  {{else if {_IS_BROWSER_TEMPLATE}}}
  const AZURE_CLIENT_ID = {__process_env_or_import_meta_env}.AZURE_CLIENT_ID ?? null;
  const AZURE_TENANT_ID = {__process_env_or_import_meta_env}.AZURE_TENANT_ID ?? null;
  {{endif}}
  const AZURE_OPENAI_API_VERSION = {__process_env_or_import_meta_env}.AZURE_OPENAI_API_VERSION ?? "<insert your Azure OpenAI API version here>";
  {{if !{_IS_OPENAI_ASST_TEMPLATE}}}
  const AZURE_OPENAI_CHAT_DEPLOYMENT = {__process_env_or_import_meta_env}.AZURE_OPENAI_CHAT_DEPLOYMENT ?? "<insert your Azure OpenAI chat deployment name here>";
  {{endif}}
  const AZURE_OPENAI_ENDPOINT = {__process_env_or_import_meta_env}.AZURE_OPENAI_ENDPOINT ?? "<insert your Azure OpenAI endpoint here>";
  {{if {_IS_OPENAI_ASST_TEMPLATE}}}
  const AZURE_OPENAI_BASE_URL = `${AZURE_OPENAI_ENDPOINT.replace(/\/+$/, '')}/openai`;
  {{else}}
  const AZURE_OPENAI_BASE_URL = `${AZURE_OPENAI_ENDPOINT.replace(/\/+$/, '')}/openai/deployments/${AZURE_OPENAI_CHAT_DEPLOYMENT}`;
  {{endif}}
{{endif}}
{{endif}}
{{if {_USE_OPENAI_CLOUD_OPENAI}}}
  const OPENAI_API_KEY = {__process_env_or_import_meta_env}.OPENAI_API_KEY ?? "<insert your OpenAI API key here>";
  {{if !{_IS_OPENAI_ASST_TEMPLATE}}}
  const OPENAI_MODEL_NAME = {__process_env_or_import_meta_env}.OPENAI_MODEL_NAME ?? "<insert your OpenAI model name here>";
  {{endif}}
  const OPENAI_ORG_ID = {__process_env_or_import_meta_env}.OPENAI_ORG_ID ?? null;
{{endif}}

{{if {_IS_AUTHOR_COMMENT} }}
// For some Learn docs and snippets, error handling or additional checks are excluded to keep
// the code scoped and focused on the main concept being showcased.
{{endif}}
{{if !{_IS_LEARN_DOC_TEMPLATE}}}
// Check if the required environment variables are set
{{if {_USE_OPENAI_CLOUD_AZURE}}}
  const azureOk = 
    {{if {_USE_AZURE_OPENAI_WITH_KEY}}}
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
    {{if {_USE_OPENAI_CLOUD_OPENAI}}}

    {{endif}}
{{endif}}
{{if {_USE_OPENAI_CLOUD_OPENAI}}}
  const openaiOk =
    {{if !{_IS_OPENAI_ASST_TEMPLATE}}}
    OPENAI_MODEL_NAME != null && !OPENAI_MODEL_NAME.startsWith('<insert') &&
    {{endif}}
    OPENAI_API_KEY != null && !OPENAI_API_KEY.startsWith('<insert');
{{endif}}

{{if {_USE_OPENAI_CLOUD_EITHER}}}
  const ok = (azureOk || openaiOk) &&
{{else if {_USE_OPENAI_CLOUD_AZURE}}}
  const ok = azureOk &&
{{else}}
  const ok = openaiOk &&
{{endif}}
{{if {_IS_OPENAI_ASST_TEMPLATE}}}
    ASSISTANT_ID != null && !ASSISTANT_ID.startsWith('<insert');
{{else}}
    AZURE_OPENAI_SYSTEM_PROMPT != null && !AZURE_OPENAI_SYSTEM_PROMPT.startsWith('<insert');
{{endif}}

  if (!ok) {
{{if {_USE_OPENAI_CLOUD_AZURE}}}
    {{if {_IS_BROWSER_TEMPLATE}}}
    chatPanelAppendMessage('computer', markdownToHtml(
    {{else}}
    console.error(
    {{endif}}
      'To use Azure OpenAI, set the following environment variables:\n' +
    {{if {_IS_OPENAI_ASST_TEMPLATE}}}
      '\n  ASSISTANT_ID' +
    {{else}}
      '\n  AZURE_OPENAI_SYSTEM_PROMPT' +
    {{endif}}
    {{if {_USE_AZURE_OPENAI_WITH_KEY}}}
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
      '\n  AZURE_OPENAI_ENDPOINT'
    {{if {_IS_BROWSER_TEMPLATE}}}
    ));
    {{else}}
    );
    {{endif}}
    {{if {_IS_BROWSER_TEMPLATE}}}
    chatPanelAppendMessage('computer', markdownToHtml(
      '\nYou can easily do that using the Azure AI CLI by doing one of the following:\n' +
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
      '\n  ai dev shell --run "npm run webpack"'
    ));
    {{else}}
    console.error(
      '\nYou can easily do that using the Azure AI CLI by doing one of the following:\n' +
      '\n  ai init' +
      '\n  ai dev shell' +
      '\n  node main.js' +
      '\n' +
      '\n  or' +
      '\n' +
      '\n  ai init' +
      '\n  ai dev shell --run "node main.js"'
    );
    {{endif}}
    {{if {_USE_OPENAI_CLOUD_OPENAI}}}

    {{endif}}
{{endif}}
{{if {_USE_OPENAI_CLOUD_OPENAI}}}
    {{if {_IS_BROWSER_TEMPLATE}}}
    chatPanelAppendMessage('computer', markdownToHtml(
    {{else}}
    console.error(
    {{endif}}
      'To use OpenAI, set the following environment variables:\n' +
      '\n  OPENAI_API_KEY' +
      '\n  OPENAI_ORG_ID (optional)' +
      {{if {_IS_OPENAI_ASST_TEMPLATE}}}
      '\n  ASSISTANT_ID'
      {{else}}
      '\n  OPENAI_MODEL_NAME' +
      '\n  AZURE_OPENAI_SYSTEM_PROMPT'
      {{endif}}
    {{if {_IS_BROWSER_TEMPLATE}}}
    ));
    {{else}}
    );
    {{endif}}
    {{if {_IS_BROWSER_TEMPLATE}}}
    chatPanelAppendMessage('computer', markdownToHtml(
    {{else}}
    console.error(
    {{endif}}
      '\nYou can easily obtain some of these values by visiting these links:\n' +
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
      '\n  ai dev shell --run "npm run webpack"'
    ));
    {{else}}
      '\n Then, do one of the following:\n' +
      '\n  ai dev shell' +
      '\n  node main.js' +
      '\n' +
      '\n  or' +
      '\n' +
      '\n  ai dev shell --run "node main.js"'
    );
    {{endif}}
{{endif}}
{{if {_IS_BROWSER_TEMPLATE}}}
    throw new Error('Missing required environment variables');
{{else}}
    process.exit(1);
{{endif}}
  }
{{endif}}