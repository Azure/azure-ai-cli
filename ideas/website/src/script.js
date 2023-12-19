const chatCompletions = require('./ChatCompletionsStreaming');

// You can replace these with your own endpoint, API key, and deployment name
const endpoint = '<insert your OpenAI endpoint here>';
const azureApiKey = '<insert your OpenAI API key here>';
const deploymentName = '<insert your OpenAI deployment name here>';
const systemPrompt = 'You are a helpful AI assistant.';

const streamingChatCompletions = new chatCompletions.StreamingChatCompletionsHelper(systemPrompt, endpoint, azureApiKey, deploymentName)

function sendMessage() {
  var userInput = document.getElementById("userInput");
  var userInputValue = userInput.value;
  if (userInputValue.trim() !== '') {
    appendMessage('user', userInputValue);
    userInput.value = '';
    updateUserInputHeight();
    updateWidthsAndHeights();
    getChatCompletions(userInputValue);
  }
}

async function getChatCompletions(userInput) {
  var newMessage = appendMessage('computer', '...');
  var computerResponse = await streamingChatCompletions.getChatCompletions(userInput, function (response) {
    newMessage.innerHTML = (newMessage.innerHTML + response).replace(/\n/g, '<br>');
  });
  newMessage.innerHTML = computerResponse.replace(/\n/g, '<br>');
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
  var themeToggleIcon = document.getElementById("toggleThemeButton").children[0];
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

toggleTheme();

document.getElementById("toggleThemeButton").addEventListener("click", toggleTheme);
document.getElementById("toggleThemeButton").addEventListener('keydown', function(event) {
  if (event.code === 'Enter' || event.code === 'Space') {
    toggleTheme();
  }
});

window.sendMessage = sendMessage;
window.toggleTheme = toggleTheme;

document.getElementById("userInput").focus();
