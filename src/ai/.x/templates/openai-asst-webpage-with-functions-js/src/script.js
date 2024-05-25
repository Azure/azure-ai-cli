const marked = require("marked");
const hljs = require("highlight.js");
const { OpenAI } = require('openai');

const { factory } = require("./OpenAIAssistantsCustomFunctions");
const { {ClassName} } = require("./OpenAIAssistantsFunctionsStreamingClass");

let assistant;
async function assistantInit(threadId = null) {

  // Which assistant, which thread?
  const ASSISTANT_ID = process.env.ASSISTANT_ID ?? "<insert your OpenAI assistant ID here>";

  {{@include openai.js/create.openai.js}}

  // Create the assistants streaming helper class instance
  assistant = new {ClassName}(ASSISTANT_ID, factory, openai);

  await assistantCreateOrRetrieveThread(threadId);
}

async function assistantProcessInput(userInput) {
  const blackVerticalRectangle = '\u25AE'; // Black vertical rectangle ('▮') to simulate an insertion point

  let newMessage = chatPanelAppendMessage('computer', blackVerticalRectangle);
  let completeResponse = "";

  await assistant.getResponse(userInput, function (response) {
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

  newMessage.innerHTML = markdownToHtml(completeResponse) || completeResponse.replace(/\n/g, '<br/>');
  chatPanel.scrollTop = chatPanel.scrollHeight;

  await threadItemsCheckIfUpdatesNeeded(userInput, completeResponse);
}

async function assistantCreateOrRetrieveThread(threadId = null) {
 
  if (threadId === null) {
    await assistant.createThread()
  } else {
    await assistant.retrieveThread(threadId);
    await assistant.getThreadMessages((role, content) => {
      let html = markdownToHtml(content) || content.replace(/\n/g, '<br/>');
      role = role === 'user' ? 'user' : 'computer';
      console.log(`role: ${role}, content: ${content}`);
      chatPanelAppendMessage(role, html);
    });
  }
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

const titleUntitled = 'Untitled';

function ThreadItem(id, created, metadata) {
  this.id = id;
  this.created = created;
  this.metadata = metadata;
}

function threadItemIsUntitled(item) {
  return item.metadata === titleUntitled;
}

async function threadItemsCheckIfUpdatesNeeded(userInput, computerResponse) {
  let items = threadItemsGet();
  threadItemsCheckMoveOrAdd(items);

  await threadItemsSetTitleIfUntitled(items, userInput, computerResponse);
}

function threadItemsCheckMoveOrAdd(items) {
  threadItemsCheckMoveTop(items, assistant.thread.id);
  threadItemsCheckAddNew(items, assistant.thread.id);
}

function threadItemsCheckMoveTop(items, threadId) {
  let item = items.find(item => item.id === threadId);
  if (item) {
    threadItemsMoveTop(items, item);
  }
}

function threadItemsMoveTop(items, item) {
  var index = items.indexOf(item);
  if (index !== -1) {
    items.splice(index, 1);
  }
  item.created = Math.floor(Date.now() / 1000);
  items.unshift(item);
  localStorage.setItem('threadItems', JSON.stringify(items));
  threadPanelPopulate(items);
}

function threadItemsCheckAddNew(items, threadId) {
  if (items.length === 0 || items[0].id !== threadId) {
    threadItemsAddNew(items, new ThreadItem(threadId, Math.floor(Date.now() / 1000), titleUntitled));
  }
}

function threadItemsAddNew(items, newItem) {
  items.unshift(newItem);
  localStorage.setItem('threadItems', JSON.stringify(items));
  threadPanelPopulate(items);
}

function threadItemsGet() {
  const threadItemsString = localStorage.getItem('threadItems');
  if (threadItemsString) {
    return JSON.parse(threadItemsString);
  } else {
    return [];
  }
}

function threadItemsLoadFakeData() {
  const now = new Date();
  const yesterday = new Date(new Date().setDate(now.getDate() - 1));
  const thirtyDaysAgo = new Date(new Date().setDate(now.getDate() - 30));

  const fakeThreadItems = [
    new ThreadItem('thread_XTqDWuGXPjsddI1xctQ2ZD4B', Math.floor(now / 1000), 'Skeleton joke'),
    new ThreadItem('thread_wzmGKFC22PKKcvoDs2zrYLD7', Math.floor(yesterday / 1000), 'Why is the sky blue?'),
    new ThreadItem('thread_IAxIrq4YJmFflA1fraw7iEcI', Math.floor(yesterday / 1000), 'Hello world in C#'),
    new ThreadItem('thread_RAgQWZFf3B3MWjVIpSO6JiRi', Math.floor(thirtyDaysAgo / 1000), 'Thread stuff'),
  ];
  return fakeThreadItems;
}

function threadItemsGetGroupName(timestamp) {
  const now = new Date();
  const itemDate = new Date(timestamp * 1000);
  const isToday = itemDate.toDateString() === now.toDateString();
  const isYesterday = itemDate.toDateString() === new Date(new Date().setDate(now.getDate() - 1)).toDateString();
  const isThisWeek = itemDate > new Date(new Date().setDate(now.getDate() - 7));
  const isThisYear = itemDate.getFullYear() === now.getFullYear();

  return isToday ? 'Today'
    : isYesterday ? 'Yesterday'
      : isThisWeek ? "Previous 7 days"
        : isThisYear ? itemDate.toLocaleDateString('en-US', { month: 'long' }) // month name
          : itemDate.toLocaleDateString('en-US', { year: 'numeric' }); // the year
}

function threadItemsGroupByDate(threadItems) {
  const groupedItems = new Map();

  threadItems.forEach(item => {
    const group = threadItemsGetGroupName(item.created);
    if (!groupedItems.has(group)) {
      groupedItems.set(group, []);
    }
    groupedItems.get(group).push(item);
  });

  return groupedItems;
}

async function threadItemsSetTitleIfUntitled(items, userInput, computerResponse) {
  if (threadItemIsUntitled(items[0])) {
    await threadItemsSetTitle(userInput, computerResponse, items, 0);
  }
}

async function threadItemsSetTitle(userInput, computerResponse, items, i) {

  // What's the system prompt?
  const AZURE_OPENAI_SYSTEM_PROMPT = process.env.AZURE_OPENAI_SYSTEM_PROMPT ?? "You are a helpful AI assistant.";

  {{set _IS_OPENAI_ASST_TEMPLATE = false}}
  {{@include openai.js/create.openai.js}}

  // Prepare the messages for the OpenAI API
  let messages = [
    { role: 'system', content: AZURE_OPENAI_SYSTEM_PROMPT },
    { role: 'user', content: userInput },
    { role: 'assistant', content: computerResponse },
    { role: 'system', content: "Please suggest a title for this interaction. Don't be cute or humorous in your answer. Answer only with a factual descriptive title. Do not use quotes. Do not prefix with 'Title:' or anything else. Just emit the title." }
  ];

  // Call the OpenAI API to get a title for the conversation
  const completion = await openai.chat.completions.create({
    messages: messages,
    {{if contains(toupper("{OPENAI_CLOUD}"), "AZURE")}}
    model: AZURE_OPENAI_CHAT_DEPLOYMENT
    {{else}}
    model: OPENAI_MODEL_NAME
    {{endif}}
  });

var newTitle = completion.choices[i].message.content;
  items[i].metadata = newTitle;

  localStorage.setItem('threadItems', JSON.stringify(items));
  threadPanelPopulate(items);
}

function threadPanelPopulate(items) {

  // Clear existing content
  const threadPanel = document.getElementById('threadPanel');
  threadPanel.innerHTML = '';

  // Group thread items by date
  const groupedThreadItems = threadItemsGroupByDate(items);

  // Iterate over grouped items and populate thread panel
  for (const [date, items] of groupedThreadItems) {
    const dateHeader = document.createElement('div');
    dateHeader.classList.add('threadOnDate');
    dateHeader.textContent = date;
    threadPanel.appendChild(dateHeader);

    const threadsContainer = document.createElement('div');
    threadsContainer.id = 'threads';
    threadPanel.appendChild(threadsContainer);

    items.forEach(item => {
      const button = document.createElement('button');
      button.id = item.id;
      button.classList.add('thread', 'w3-button');
      button.onclick = function() {
        loadThread(this.id);
      };

      const div = document.createElement('div');
      const icon = document.createElement('i');
      icon.classList.add('threadIcon', 'fa', 'fa-comment');

      div.appendChild(icon);
      div.appendChild(document.createTextNode(item.metadata));
      button.appendChild(div);
      threadsContainer.appendChild(button);
    });
  }
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

async function newChat() {
  chatPanelClear();
  logoShow();
  userInputTextAreaFocus();
  await assistantCreateOrRetrieveThread();
}

async function loadThread(threadId) {
  chatPanelClear();
  await assistantCreateOrRetrieveThread(threadId);
  userInputTextAreaFocus();
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
    assistantProcessInput(inputValue);
  }
}

async function init() {

  const urlParams = new URLSearchParams(window.location.search);

  themeInit();
  markdownInit();
  userInputTextAreaInit();
  varsInit();

  let items;
  await assistantInit();

  const fake = urlParams.get('fake') === 'true';
  if (fake) {
    items = threadItemsLoadFakeData();
    localStorage.setItem('threadItems', JSON.stringify(items));
  }

  const clear = urlParams.get('clear') === 'true';
  if (clear) {
    localStorage.removeItem('threadItems');
    items = [];
  }

  items = items || threadItemsGet();
  threadPanelPopulate(items);

  userInputTextAreaFocus();

  window.newChat = newChat;
  window.loadThread = loadThread;
  window.sendMessage = sendMessage;
  window.toggleTheme = toggleTheme;
}

init();