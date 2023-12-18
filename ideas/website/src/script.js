const chatCompletions = require('./ChatCompletionsStreaming');

// You can replace these with your own endpoint, API key, and deployment name
const endpoint = '<insert your OpenAI endpoint here>';
const azureApiKey = '<insert your OpenAI API key here>';
const deploymentName = '<insert your OpenAI deployment name here>';
const systemPrompt = 'You are a helpful AI assistant.';

function sendMessage() {
  var userInput = document.getElementById('userInput').value;
  if (userInput.trim() !== '') {
    appendMessage('user', userInput);
    document.getElementById('userInput').value = '';
    updateUserInputHeight();
    updateWidthsAndHeights();
    getChatCompletions(userInput);
  }
}

async function getChatCompletions(userInput) {
  var computerResponse = await chatCompletions.getChatCompletions(userInput, systemPrompt, endpoint, azureApiKey, deploymentName);
  appendMessage('computer', computerResponse);
}

function appendMessage(sender, message) {
  var logo = document.getElementById("logo");
  logo.style.display = "none";

  message = message.replace(/\n/g, '<br>');
  var chatPanel = document.getElementById("chatPanel");
  var newMessage = document.createElement("p");
  newMessage.className = sender === "user" ? "w3-padding user" : "w3-padding computer";
  newMessage.innerHTML = message;
  chatPanel.appendChild(newMessage);
  chatPanel.scrollTop = chatPanel.scrollHeight;

  return newMessage;
}

document.getElementById("userInput").addEventListener("keydown", function(event) {
  if (event.key === "Enter") {
    if (!event.shiftKey) {
      event.preventDefault();
      sendMessage();
    }
  }
});

document.getElementById("userInput").addEventListener("input", updateUserInputHeight);

function updateUserInputHeight() {
  var userInput = document.getElementById("userInput");
  userInput.style.height = 'auto';
  userInput.style.height = (userInput.scrollHeight) + 'px';
}

function updateWidthsAndHeights() {
  var headerHeight = document.querySelector('#header').offsetHeight;
  var userInputHeight = document.querySelector('#userInputPanel').offsetHeight;
  var sendButtonWidth = document.querySelector('#sendButton').offsetWidth;
  var leftSideWidth = document.querySelector('#leftSide').offsetWidth;
  var rightSideWidth = document.querySelector('#rightSide').offsetWidth;
  document.documentElement.style.setProperty('--header-height', headerHeight + 'px');
  document.documentElement.style.setProperty('--input-height', userInputHeight + 'px');
  document.documentElement.style.setProperty('--send-button-width', sendButtonWidth + 'px');
  document.documentElement.style.setProperty('--left-side-width', leftSideWidth + 'px');
  document.documentElement.style.setProperty('--right-side-width', rightSideWidth + 'px');
}

window.addEventListener('resize', updateWidthsAndHeights);
document.addEventListener('DOMContentLoaded', updateWidthsAndHeights);

function toggleTheme() {
  var body = document.body;
  var themeToggle = document.getElementById("toggleThemeButton").children[0];
  if (body.classList.contains("light-theme")) {
    body.classList.remove("light-theme");
    themeToggle.classList.remove("fa-toggle-off");
    themeToggle.classList.add("fa-toggle-on");
  } else {
    body.classList.add("light-theme");
    themeToggle.classList.remove("fa-toggle-on");
    themeToggle.classList.add("fa-toggle-off");
  }
}

toggleTheme();

window.sendMessage = sendMessage;
window.toggleTheme = toggleTheme;