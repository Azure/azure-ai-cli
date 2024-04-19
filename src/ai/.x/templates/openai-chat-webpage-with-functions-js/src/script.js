<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_API_VERSION" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
<#@ parameter type="System.String" name="OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="OPENAI_ORG_ID" #>
<#@ parameter type="System.String" name="OPENAI_MODEL_NAME" #>
const marked = require("marked");
const hljs = require("highlight.js");

const { factory } = require("./OpenAIChatCompletionsCustomFunctions");
const { CreateOpenAI } = require("./CreateOpenAI");
const { <#= ClassName #> } = require('./OpenAIChatCompletionsFunctionsStreamingClass');

// NOTE: Never deploy your key in client-side environments like browsers or mobile apps
//  SEE: https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety

// What's the system prompt
const openAISystemPrompt = process.env.AZURE_OPENAI_SYSTEM_PROMPT || "<#= AZURE_OPENAI_SYSTEM_PROMPT #>";

// Connection info
const azureClientId = process.env.AZURE_CLIENT_ID || null;
const azureTenantId = process.env.AZURE_TENANT_ID || null;
const azureOpenAIAPIKey = process.env.AZURE_OPENAI_API_KEY || "<#= AZURE_OPENAI_API_KEY #>";
const azureOpenAIAPIVersion = process.env.AZURE_OPENAI_API_VERSION || "<#= AZURE_OPENAI_API_VERSION #>";
const azureOpenAIEndpoint = process.env.AZURE_OPENAI_ENDPOINT || "<#= AZURE_OPENAI_ENDPOINT #>";
const azureOpenAIChatDeploymentName = process.env.AZURE_OPENAI_CHAT_DEPLOYMENT || "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>";
const openAIAPIKey = process.env.OPENAI_API_KEY || "<#= OPENAI_API_KEY #>";
const openAIOrganization = process.env.OPENAI_ORG_ID || null;
const openAIModelName = process.env.OPENAI_MODEL_NAME || "<#= OPENAI_MODEL_NAME #>";

const useAzure = azureOpenAIEndpoint?.startsWith("https://");

let streamingChatCompletions;
async function streamingChatCompletionsInit() {

  // Check the connection info
  const azureOk = !azureOpenAIAPIVersion?.startsWith('<insert') && !azureOpenAIEndpoint?.startsWith('<insert') && !azureOpenAIChatDeploymentName?.startsWith('<insert');
  const azureAADOk = azureOk && azureClientId && azureTenantId;
  const azureKeyOk = azureOk && !azureOpenAIAPIKey?.startsWith('<insert');
  const openaiOk = !openAIAPIKey?.startsWith('<insert') && !openAIModelName.startsWith('<insert');
  if (!azureAADOk && !azureKeyOk && !openaiOk) {
    chatPanelAppendMessage('computer', markdownToHtml('To use **OpenAI**, set `OPENAI_API_KEY` and `OPENAI_MODEL_NAME` in `.env`'));
    chatPanelAppendMessage('computer', markdownToHtml('To use **Azure OpenAI**, set `AZURE_OPENAI_API_VERSION`, `AZURE_OPENAI_ENDPOINT`, and `AZURE_OPENAI_CHAT_DEPLOYMENT` in `.env`'));
    chatPanelAppendMessage('computer', markdownToHtml('For Azure OpenAI **w/ AAD**,  set `AZURE_CLIENT_ID` and `AZURE_TENANT_ID` in `.env`'));
    chatPanelAppendMessage('computer', markdownToHtml('For Azure OpenAI **w/ API KEY**, set `AZURE_OPENAI_API_KEY` in `.env`'));
  }

  // Create the OpenAI client
  const openai = useAzure
    ? await CreateOpenAI.fromAzureOpenAIEndpointAndDeployment(azureOpenAIEndpoint, azureOpenAIAPIVersion, azureOpenAIChatDeploymentName, azureOpenAIAPIKey, azureClientId, azureTenantId)
    : CreateOpenAI.fromOpenAIKey(openAIAPIKey, openAIOrganization);

  // Create the streaming chat completions helper
  streamingChatCompletions = useAzure
    ? new <#= ClassName #>(azureOpenAIChatDeploymentName, openAISystemPrompt, factory, openai, 20)
    : new <#= ClassName #>(openAIModelName, openAISystemPrompt, factory, openai);
}

function streamingChatCompletionsClear() {
  streamingChatCompletions.clearConversation();
}

async function streamingChatCompletionsProcessInput(userInput) {
  const blackVerticalRectangle = '\u25AE'; // Black vertical rectangle ('▮') to simulate an insertion point

  let newMessage = chatPanelAppendMessage('computer', blackVerticalRectangle);
  let completeResponse = "";

  let computerResponse = await streamingChatCompletions.getResponse(userInput, function (response) {
    let atBottomBeforeUpdate = chatPanelIsScrollAtBottom();

    completeResponse += response;
    let withEnding = `${completeResponse}${blackVerticalRectangle}`;
    let asHtml = markdownToHtml(withEnding);

    if (asHtml !== undefined) {
      newMessage.innerHTML = asHtml;

      if (atBottomBeforeUpdate) {
        chatPanelScrollToBottom();
      }
    }
  });

  newMessage.innerHTML = markdownToHtml(computerResponse) || computerResponse.replace(/\n/g, '<br/>');
  chatPanel.scrollTop = chatPanel.scrollHeight;
}

function chatPanelGetElement() {
  return document.getElementById("chatPanel");
}

function chatPanelAppendMessage(sender, message) {
  logoHide();

  let messageContent = document.createElement("p");
  messageContent.className = "message-content";
  messageContent.innerHTML = message;

  let messageAuthor = document.createElement("p");
  messageAuthor.className = "message-author";
  messageAuthor.innerHTML = sender == "user" ? "You" : "Assistant";

  let divContainingBoth = document.createElement("div");
  divContainingBoth.className = sender === "user" ? "user" : "computer";
  divContainingBoth.appendChild(messageAuthor);
  divContainingBoth.appendChild(messageContent);

  let chatPanel = chatPanelGetElement();
  chatPanel.appendChild(divContainingBoth);
  chatPanelScrollToBottom();

  return messageContent;
}

function chatPanelIsScrollAtBottom() {
  let chatPanel = chatPanelGetElement();
  let atBottom = Math.abs(chatPanel.scrollHeight - chatPanel.clientHeight - chatPanel.scrollTop) < 1;
  return atBottom;
}

function chatPanelScrollToBottom() {
  let chatPanel = chatPanelGetElement();
  chatPanel.scrollTop = chatPanel.scrollHeight;
}

function chatPanelClear() {
  let chatPanel = chatPanelGetElement();
  chatPanel.innerHTML = '';
}

function logoGetElement() {
  return document.getElementById("logo");
}

function logoShow() {
  let logo = logoGetElement();
  logo.style.display = "block";
}

function logoHide() {
  let logo = logoGetElement();
  logo.style.display = "none";
}

function markdownInit() {
  marked.setOptions({
    highlight: function (code, lang) {
      let hl = lang === undefined || lang === ''
        ? hljs.highlightAuto(code).value
        : hljs.highlight(lang, code).value;
      return `<div class="hljs">${hl}</div>`;
    }
  });
}

function markdownToHtml(markdownText) {
  try {
    return marked.parse(markdownText);
  }
  catch (error) {
    return undefined;
  }
}

function themeInit() {
  let currentTheme = localStorage.getItem('theme');
  if (currentTheme === 'dark') {
    themeSetDark();
  }
  else if (currentTheme === 'light') {
    themeSetLight();
  }
  toggleThemeButtonInit();
}

function themeIsLight() {
  return document.body.classList.contains("light-theme");
}

function themeIsDark() {
  return !themeIsLight();
}

function toggleTheme() {
  if (themeIsLight()) {
    themeSetDark();
  } else {
    themeSetLight();
  }
}

function themeSetLight() {
  if (!themeIsLight()) {
    document.body.classList.add("light-theme");
    localStorage.setItem('theme', 'light');

    let iconElement = toggleThemeButtonGetElement().children[0];
    iconElement.classList.remove("fa-toggle-on");
    iconElement.classList.add("fa-toggle-off");
  }
}

function themeSetDark() {
  if (!themeIsDark()) {
    document.body.classList.remove("light-theme");
    localStorage.setItem('theme', 'dark');

    let iconElement = toggleThemeButtonGetElement().children[0];
    iconElement.classList.remove("fa-toggle-off");
    iconElement.classList.add("fa-toggle-on");
  }
}

function toggleThemeButtonGetElement() {
  return document.getElementById("toggleThemeButton");
}

function toggleThemeButtonInit() {
  let buttonElement = toggleThemeButtonGetElement();
  buttonElement.addEventListener("click", toggleTheme);
  buttonElement.addEventListener('keydown', toggleThemeButtonHandleKeyDown());
}

function toggleThemeButtonHandleKeyDown() {
  return function (event) {
    if (event.code === 'Enter' || event.code === 'Space') {
      toggleTheme();
    }
  };
}

function userInputTextAreaGetElement() {
  return document.getElementById("userInput");
}

function userInputTextAreaInit() {
  let inputElement = userInputTextAreaGetElement();
  inputElement.addEventListener("keydown", userInputTextAreaHandleKeyDown());
  inputElement.addEventListener("input", userInputTextAreaUpdateHeight);
}

function userInputTextAreaFocus() {
  let inputElement = userInputTextAreaGetElement();
  inputElement.focus();
}

function userInputTextAreaClear() {
  userInputTextAreaGetElement().value = '';
  userInputTextAreaUpdateHeight();
}

function userInputTextAreaUpdateHeight() {
  let inputElement = userInputTextAreaGetElement();
  inputElement.style.height = 'auto';
  inputElement.style.height = (userInput.scrollHeight) + 'px';
}

function userInputTextAreaHandleKeyDown() {
  return function (event) {
    if (event.key === "Enter") {
      if (!event.shiftKey) {
        event.preventDefault();
        sendMessage();
      }
    }
  };
}

function varsInit() {
  document.addEventListener('DOMContentLoaded', varsUpdateHeightsAndWidths);
  window.addEventListener('resize', varsUpdateHeightsAndWidths);
}

function varsUpdateHeightsAndWidths() {
  let headerHeight = document.querySelector('#header').offsetHeight;
  let userInputHeight = document.querySelector('#userInputPanel').offsetHeight;
  document.documentElement.style.setProperty('--header-height', headerHeight + 'px');
  document.documentElement.style.setProperty('--input-height', userInputHeight + 'px');
}

function newChat() {
  chatPanelClear();
  logoShow();
  userInputTextAreaFocus();
  streamingChatCompletionsClear();
}

function sendMessage() {
  let inputElement = userInputTextAreaGetElement();
  let inputValue = inputElement.value;

  let notEmpty = inputValue.trim() !== '';
  if (notEmpty) {
    let html = markdownToHtml(inputValue) || inputValue.replace(/\n/g, '<br/>');
    chatPanelAppendMessage('user', html);
    userInputTextAreaClear();
    varsUpdateHeightsAndWidths();
    streamingChatCompletionsProcessInput(inputValue);
  }
}

themeInit();
markdownInit();
userInputTextAreaInit();
varsInit();
streamingChatCompletionsInit();
userInputTextAreaFocus();

window.sendMessage = sendMessage;
window.toggleTheme = toggleTheme;
window.newChat = newChat;
