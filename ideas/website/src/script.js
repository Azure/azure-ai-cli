const chatCompletions = require('./ChatCompletionsStreaming');
const marked = require("marked");
const hljs = require("highlight.js");

function newChat() {
  let chatPanel = document.getElementById("chatPanel");
  chatPanel.innerHTML = '';
  let logo = document.getElementById("logo");
  logo.style.display = "block";
  document.getElementById("userInput").focus();
  streamingChatCompletions.clearConversation();
}

function sendMessage() {
  let userInput = document.getElementById("userInput");
  let userInputValue = userInput.value;
  if (userInputValue.trim() !== '') {
    appendMessage('user', markdownToHtml(userInputValue));
    userInput.value = '';
    updateUserInputHeight();
    updateWidthsAndHeights();
    getChatCompletions(userInputValue);
  }
}

async function getChatCompletions(userInput) {
  let ending = '◼️';
  let newMessage = appendMessage('computer', ending);
  let computerResponse = await streamingChatCompletions.getChatCompletions(userInput, function (response) {
    let chatPanel = document.getElementById("chatPanel");
    let atBottomNow = Math.abs(chatPanel.scrollHeight - chatPanel.clientHeight - chatPanel.scrollTop) < 1;

    let newContent = newMessage.innerHTML.replace(ending, response) + ending;
    newMessage.innerHTML = newContent.replace(/\n/g, '<br>');

    if (atBottomNow) {
      chatPanel.scrollTop = chatPanel.scrollHeight;
    }
  });

  newMessage.innerHTML = markdownToHtml(computerResponse);
  chatPanel.scrollTop = chatPanel.scrollHeight;
}

function markdownToHtml(markdownText) {
  return marked.parse(markdownText);
}

function appendMessage(sender, message) {
  let logo = document.getElementById("logo");
  logo.style.display = "none";

  let p = document.createElement("p");
  p.className = "message-content";
  p.innerHTML = message;

  let p2 = document.createElement("p");
  p2.className = "message-author";
  p2.innerHTML = sender == "user" ? "You" : "Assistant";

  let d = document.createElement("div");
  d.className = sender === "user" ? "user" : "computer";
  d.appendChild(p2);
  d.appendChild(p);

  let chatPanel = document.getElementById("chatPanel");
  chatPanel.appendChild(d);
  chatPanel.scrollTop = chatPanel.scrollHeight;

  return p;
}

function updateUserInputHeight() {
  let userInput = document.getElementById("userInput");
  userInput.style.height = 'auto';
  userInput.style.height = (userInput.scrollHeight) + 'px';
}

function updateWidthsAndHeights() {
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

function toggleTheme() {
  let body = document.body;
  let themeToggleIcon = document.getElementById("toggleThemeButton").children[0];
  if (body.classList.contains("light-theme")) {
    body.classList.remove("light-theme");
    themeToggleIcon.classList.remove("fa-toggle-off");
    themeToggleIcon.classList.add("fa-toggle-on");
  } else {
    body.classList.add("light-theme");
    themeToggleIcon.classList.remove("fa-toggle-on");
    themeToggleIcon.classList.add("fa-toggle-off");
  }
}

function userInputHandleKeyDown() {
  return function (event) {
    if (event.key === "Enter") {
      if (!event.shiftKey) {
        event.preventDefault();
        sendMessage();
      }
    }
  };
}

function toggleThemeButtonHandleKeyDown() {
  return function (event) {
    if (event.code === 'Enter' || event.code === 'Space') {
      toggleTheme();
    }
  };
}

marked.setOptions({
  highlight: function(code, lang) {
    let hl = lang === undefined || lang === ''
      ? hljs.highlightAuto(code).value
      : hljs.highlight(lang, code).value;
    return `<div class="hljs">${hl}</div>`;
  }
});

document.getElementById("userInput").addEventListener("keydown", userInputHandleKeyDown());
document.getElementById("userInput").addEventListener("input", updateUserInputHeight);
document.addEventListener('DOMContentLoaded', updateWidthsAndHeights);
window.addEventListener('resize', updateWidthsAndHeights);

document.getElementById("toggleThemeButton").addEventListener("click", toggleTheme);
document.getElementById("toggleThemeButton").addEventListener('keydown', toggleThemeButtonHandleKeyDown());

window.sendMessage = sendMessage;
window.toggleTheme = toggleTheme;
window.newChat = newChat;

toggleTheme();
document.getElementById("userInput").focus();

const endpoint = process.env.OPENAI_ENDPOINT;
const azureApiKey = process.env.OPENAI_API_KEY;
const deploymentName = process.env.AZURE_OPENAI_CHAT_DEPLOYMENT;
const systemPrompt = "You are a helpful AI assistant."

if (!endpoint) {
  appendMessage('computer', 'Please set OPENAI_ENDPOINT in .env');
}
if (!azureApiKey) {
  appendMessage('computer', 'Please set OPENAI_API_KEY in .env');
}
if (!deploymentName) {
  appendMessage('computer', 'Please set AZURE_OPENAI_CHAT_DEPLOYMENT in .env');
}

const streamingChatCompletions = new chatCompletions.StreamingChatCompletionsHelper(systemPrompt, endpoint, azureApiKey, deploymentName)