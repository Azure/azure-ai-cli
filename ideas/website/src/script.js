const marked = require("marked");
const hljs = require("highlight.js");

const chatCompletions = require('./ChatCompletionsStreaming');
const streamingChatIncompleteEnding = '◼️';
let streamingChatCompletions;

function streamingChatCompletionsInit() {
  const endpoint = process.env.OPENAI_ENDPOINT;
  const azureApiKey = process.env.OPENAI_API_KEY;
  const deploymentName = process.env.AZURE_OPENAI_CHAT_DEPLOYMENT;
  const systemPrompt = "You are a helpful AI assistant.";

  if (!endpoint) {
    chatPanelAppendMessage('computer', 'Please set OPENAI_ENDPOINT in .env');
  }
  if (!azureApiKey) {
    chatPanelAppendMessage('computer', 'Please set OPENAI_API_KEY in .env');
  }
  if (!deploymentName) {
    chatPanelAppendMessage('computer', 'Please set AZURE_OPENAI_CHAT_DEPLOYMENT in .env');
  }

  streamingChatCompletions = new chatCompletions.StreamingChatCompletionsHelper(systemPrompt, endpoint, azureApiKey, deploymentName);
}

function streamingChatCompletionsClear() {
  streamingChatCompletions.clearConversation();
}

async function streamingChatCompletionsProcessInput(userInput) {
  let newMessage = chatPanelAppendMessage('computer', streamingChatIncompleteEnding);

  let computerResponse = await streamingChatCompletions.getChatCompletions(userInput, function (response) {
    let atBottomBeforeUpdate = chatPanelIsScrollAtBottom();

    let newContent = newMessage.innerHTML.replace(streamingChatIncompleteEnding, response) + streamingChatIncompleteEnding;
    newMessage.innerHTML = newContent.replace(/\n/g, '<br>');

    if (atBottomBeforeUpdate) {
      chatPanelScrollToBottom();
    }
  });

  newMessage.innerHTML = markdownToHtml(computerResponse);
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
  return marked.parse(markdownText);
}

function themeToggle() {
  let iconElement = toggleThemeButtonGetElement().children[0];
  let bodyElement = document.body;
  if (bodyElement.classList.contains("light-theme")) {
    bodyElement.classList.remove("light-theme");
    iconElement.classList.remove("fa-toggle-off");
    iconElement.classList.add("fa-toggle-on");
  } else {
    bodyElement.classList.add("light-theme");
    iconElement.classList.remove("fa-toggle-on");
    iconElement.classList.add("fa-toggle-off");
  }
}

function toggleThemeButtonGetElement() {
  return document.getElementById("toggleThemeButton");
}

function toggleThemeButtonInit() {
  let buttonElement = toggleThemeButtonGetElement();
  buttonElement.addEventListener("click", themeToggle);
  buttonElement.addEventListener('keydown', toggleThemeButtonHandleKeyDown());
}

function toggleThemeButtonHandleKeyDown() {
  return function (event) {
    if (event.code === 'Enter' || event.code === 'Space') {
      themeToggle();
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
  let sendButtonWidth = document.querySelector('#sendButton').offsetWidth;
  let leftSideWidth = document.querySelector('#leftSide').offsetWidth;
  let rightSideWidth = document.querySelector('#rightSide').offsetWidth;
  document.documentElement.style.setProperty('--header-height', headerHeight + 'px');
  document.documentElement.style.setProperty('--input-height', userInputHeight + 'px');
  document.documentElement.style.setProperty('--send-button-width', sendButtonWidth + 'px');
  document.documentElement.style.setProperty('--left-side-width', leftSideWidth + 'px');
  document.documentElement.style.setProperty('--right-side-width', rightSideWidth + 'px');
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
    let html = markdownToHtml(inputValue);
    chatPanelAppendMessage('user', html);
    userInputTextAreaClear();
    varsUpdateHeightsAndWidths();
    streamingChatCompletionsProcessInput(inputValue);
  }
}

markdownInit();
toggleThemeButtonInit();
userInputTextAreaInit();
varsInit();
streamingChatCompletionsInit();

themeToggle();
userInputTextAreaFocus();

window.sendMessage = sendMessage;
window.toggleTheme = themeToggle;
window.newChat = newChat;
