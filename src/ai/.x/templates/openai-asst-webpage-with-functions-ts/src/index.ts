import { marked } from "marked";
import hljs from "highlight.js";
import { OpenAI } from 'openai';

import { factory } from "./OpenAIAssistantsCustomFunctions";
import { {ClassName} } from "./OpenAIAssistantsFunctionsStreamingClass";

let assistant: {ClassName};
async function assistantInit(threadId: string | null = null): Promise<void> {

  // Which assistant, which thread?
  const ASSISTANT_ID: string = process.env.ASSISTANT_ID ?? "<insert your OpenAI assistant ID here>";

  {{@include openai.asst.or.chat.create.openai.node.js}}

  // Create the assistants streaming helper class instance
  assistant = new {ClassName}(ASSISTANT_ID, factory, openai);

  await assistantCreateOrRetrieveThread(threadId);
}

async function assistantProcessInput(userInput: string): Promise<void> {
  const blackVerticalRectangle: string = '\u25AE'; // Black vertical rectangle ('▮') to simulate an insertion point

  let newMessage: HTMLElement = chatPanelAppendMessage('computer', blackVerticalRectangle);
  let completeResponse: string = "";

  await assistant.getResponse(userInput, function (response: string): void {
    let atBottomBeforeUpdate: boolean = chatPanelIsScrollAtBottom();

    completeResponse += response;
    let withEnding: string = `${completeResponse}${blackVerticalRectangle}`;
    let asHtml: string | undefined = markdownToHtml(withEnding);

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

async function assistantCreateOrRetrieveThread(threadId: string | null = null): Promise<void> {
 
  if (threadId === null) {
    await assistant.createThread()
  } else {
    await assistant.retrieveThread(threadId);
    await assistant.getThreadMessages((role: string, content: string): void => {
      let html: string | undefined = markdownToHtml(content) || content.replace(/\n/g, '<br/>');
      role = role === 'user' ? 'user' : 'computer';
      console.log(`role: ${role}, content: ${content}`);
      chatPanelAppendMessage(role, html);
    });
  }
}

function chatPanelGetElement(): HTMLElement | null {
  return document.getElementById("chatPanel");
}

function chatPanelAppendMessage(sender: string, message: string): HTMLElement {
  logoHide();

  let messageContent: HTMLElement = document.createElement("p");
  messageContent.className = "message-content";
  messageContent.innerHTML = message;

  let messageAuthor: HTMLElement = document.createElement("p");
  messageAuthor.className = "message-author";
  messageAuthor.innerHTML = sender == "user" ? "You" : "Assistant";

  let divContainingBoth: HTMLElement = document.createElement("div");
  divContainingBoth.className = sender === "user" ? "user" : "computer";
  divContainingBoth.appendChild(messageAuthor);
  divContainingBoth.appendChild(messageContent);

  let chatPanel: HTMLElement | null = chatPanelGetElement();
  chatPanel.appendChild(divContainingBoth);
  chatPanelScrollToBottom();

  return messageContent;
}

function chatPanelIsScrollAtBottom(): boolean {
  let chatPanel: HTMLElement | null = chatPanelGetElement();
  let atBottom: boolean = Math.abs(chatPanel.scrollHeight - chatPanel.clientHeight - chatPanel.scrollTop) < 1;
  return atBottom;
}

function chatPanelScrollToBottom(): void {
  let chatPanel: HTMLElement | null = chatPanelGetElement();
  chatPanel.scrollTop = chatPanel.scrollHeight;
}

function chatPanelClear(): void {
  let chatPanel: HTMLElement | null = chatPanelGetElement();
  chatPanel.innerHTML = '';
}

function logoGetElement(): HTMLElement | null {
  return document.getElementById("logo");
}

function logoShow(): void {
  let logo: HTMLElement | null = logoGetElement();
  logo.style.display = "block";
}

function logoHide(): void {
  let logo: HTMLElement | null = logoGetElement();
  logo.style.display = "none";
}

function markdownInit(): void {
  marked.setOptions({
    highlight: function (code: string, lang: string): string {
      let hl: string = lang === undefined || lang === ''
        ? hljs.highlightAuto(code).value
        : hljs.highlight(lang, code).value;
      return `<div class="hljs">${hl}</div>`;
    }
  });
}

function markdownToHtml(markdownText: string): string | undefined {
  try {
    return marked.parse(markdownText);
  }
  catch (error) {
    return undefined;
  }
}

function themeInit(): void {
  let currentTheme: string | null = localStorage.getItem('theme');
  if (currentTheme === 'dark') {
    themeSetDark();
  }
  else if (currentTheme === 'light') {
    themeSetLight();
  }
  toggleThemeButtonInit();
}

function themeIsLight(): boolean {
  return document.body.classList.contains("light-theme");
}

function themeIsDark(): boolean {
  return !themeIsLight();
}

function toggleTheme(): void {
  if (themeIsLight()) {
    themeSetDark();
  } else {
    themeSetLight();
  }
}

function themeSetLight(): void {
  if (!themeIsLight()) {
    document.body.classList.add("light-theme");
    localStorage.setItem('theme', 'light');

    let iconElement: Element = toggleThemeButtonGetElement().children[0];
    iconElement.classList.remove("fa-toggle-on");
    iconElement.classList.add("fa-toggle-off");
  }
}

function themeSetDark(): void {
  if (!themeIsDark()) {
    document.body.classList.remove("light-theme");
    localStorage.setItem('theme', 'dark');

    let iconElement: Element = toggleThemeButtonGetElement().children[0];
    iconElement.classList.remove("fa-toggle-off");
    iconElement.classList.add("fa-toggle-on");
  }
}

function toggleThemeButtonGetElement(): HTMLElement {
  return document.getElementById("toggleThemeButton");
}

function toggleThemeButtonInit(): void {
  let buttonElement: HTMLElement = toggleThemeButtonGetElement();
  buttonElement.addEventListener("click", toggleTheme);
  buttonElement.addEventListener('keydown', toggleThemeButtonHandleKeyDown());
}

function toggleThemeButtonHandleKeyDown(): (event: KeyboardEvent) => void {
  return function (event: KeyboardEvent): void {
    if (event.code === 'Enter' || event.code === 'Space') {
      toggleTheme();
    }
  };
}

const titleUntitled: string = 'Untitled';

interface ThreadItem {
  id: string;
  created: number;
  metadata: string;
}

function threadItemIsUntitled(item: ThreadItem): boolean {
  return item.metadata === titleUntitled;
}

async function threadItemsCheckIfUpdatesNeeded(userInput: string, computerResponse: string): Promise<void> {
  let items: ThreadItem[] = threadItemsGet();
  threadItemsCheckMoveOrAdd(items);

  await threadItemsSetTitleIfUntitled(items, userInput, computerResponse);
}

function threadItemsCheckMoveOrAdd(items: ThreadItem[]): void {
  threadItemsCheckMoveTop(items, assistant.thread.id);
  threadItemsCheckAddNew(items, assistant.thread.id);
}

function threadItemsCheckMoveTop(items: ThreadItem[], threadId: string): void {
  let item: ThreadItem | undefined = items.find(item => item.id === threadId);
  if (item) {
    threadItemsMoveTop(items, item);
  }
}

function threadItemsMoveTop(items: ThreadItem[], item: ThreadItem): void {
  var index: number = items.indexOf(item);
  if (index !== -1) {
    items.splice(index, 1);
  }
  item.created = Math.floor(Date.now() / 1000);
  items.unshift(item);
  localStorage.setItem('threadItems', JSON.stringify(items));
  threadPanelPopulate(items);
}

function threadItemsCheckAddNew(items: ThreadItem[], threadId: string): void {
  if (items.length === 0 || items[0].id !== threadId) {
    threadItemsAddNew(items, { id: threadId, created: Math.floor(Date.now() / 1000), metadata: titleUntitled });
  }
}

function threadItemsAddNew(items: ThreadItem[], newItem: ThreadItem): void {
  items.unshift(newItem);
  localStorage.setItem('threadItems', JSON.stringify(items));
  threadPanelPopulate(items);
}

function threadItemsGet(): ThreadItem[] {
  const threadItemsString: string | null = localStorage.getItem('threadItems');
  if (threadItemsString) {
    return JSON.parse(threadItemsString);
  } else {
    return [];
  }
}

function threadItemsLoadFakeData(): ThreadItem[] {
  const now: Date = new Date();
  const yesterday: Date = new Date(new Date().setDate(now.getDate() - 1));
  const thirtyDaysAgo: Date = new Date(new Date().setDate(now.getDate() - 30));

  const fakeThreadItems: ThreadItem[] = [
    { id: 'thread_XTqDWuGXPjsddI1xctQ2ZD4B', created: Math.floor(now.getTime() / 1000), metadata: 'Skeleton joke' },
    { id: 'thread_wzmGKFC22PKKcvoDs2zrYLD7', created: Math.floor(yesterday.getTime() / 1000), metadata: 'Why is the sky blue?' },
    { id: 'thread_IAxIrq4YJmFflA1fraw7iEcI', created: Math.floor(yesterday.getTime() / 1000), metadata: 'Hello world in C#' },
    { id: 'thread_RAgQWZFf3B3MWjVIpSO6JiRi', created: Math.floor(thirtyDaysAgo.getTime() / 1000), metadata: 'Thread stuff' },
  ];
  return fakeThreadItems;
}

function threadItemsGetGroupName(timestamp: number): string {
  const now: Date = new Date();
  const itemDate: Date = new Date(timestamp * 1000);
  const isToday: boolean = itemDate.toDateString() === now.toDateString();
  const isYesterday: boolean = itemDate.toDateString() === new Date(new Date().setDate(now.getDate() - 1)).toDateString();
  const isThisWeek: boolean = itemDate > new Date(new Date().setDate(now.getDate() - 7));
  const isThisYear: boolean = itemDate.getFullYear() === now.getFullYear();

  return isToday ? 'Today'
    : isYesterday ? 'Yesterday'
      : isThisWeek ? "Previous 7 days"
        : isThisYear ? itemDate.toLocaleDateString('en-US', { month: 'long' }) // month name
          : itemDate.toLocaleDateString('en-US', { year: 'numeric' }); // the year
}

function threadItemsGroupByDate(threadItems: ThreadItem[]): Map<string, ThreadItem[]> {
  const groupedItems: Map<string, ThreadItem[]> = new Map();

  threadItems.forEach(item => {
    const group: string = threadItemsGetGroupName(item.created);
    if (!groupedItems.has(group)) {
      groupedItems.set(group, []);
    }
    groupedItems.get(group).push(item);
  });

  return groupedItems;
}

async function threadItemsSetTitleIfUntitled(items: ThreadItem[], userInput: string, computerResponse: string): Promise<void> {
  if (threadItemIsUntitled(items[0])) {
    await threadItemsSetTitle(userInput, computerResponse, items, 0);
  }
}

async function threadItemsSetTitle(userInput: string, computerResponse: string, items: ThreadItem[], i: number): Promise<void> {

  // What's the system prompt?
  const AZURE_OPENAI_SYSTEM_PROMPT: string = process.env.AZURE_OPENAI_SYSTEM_PROMPT ?? "You are a helpful AI assistant.";

  {{set _IS_OPENAI_ASST_TEMPLATE = false}}
  {{@include openai.asst.or.chat.create.openai.node.js}}

  // Prepare the messages for the OpenAI API
  let messages: any[] = [
    { role: 'system', content: AZURE_OPENAI_SYSTEM_PROMPT },
    { role: 'user', content: userInput },
    { role: 'assistant', content: computerResponse },
    { role: 'system', content: "Please suggest a title for this interaction. Don't be cute or humorous in your answer. Answer only with a factual descriptive title. Do not use quotes. Do not prefix with 'Title:' or anything else. Just emit the title." }
  ];

  // Call the OpenAI API to get a title for the conversation
  const completion = await openai.chat.completions.create({
    messages: messages,
    {{if {USE_AZURE_OPENAI}}}
    model: AZURE_OPENAI_CHAT_DEPLOYMENT
    {{else}}
    model: OPENAI_MODEL_NAME
    {{endif}}
  });

var newTitle: string = completion.choices[i].message.content;
  items[i].metadata = newTitle;

  localStorage.setItem('threadItems', JSON.stringify(items));
  threadPanelPopulate(items);
}

function threadPanelPopulate(items: ThreadItem[]): void {

  // Clear existing content
  const threadPanel: HTMLElement = document.getElementById('threadPanel');
  threadPanel.innerHTML = '';

  // Group thread items by date
  const groupedThreadItems: Map<string, ThreadItem[]> = threadItemsGroupByDate(items);

  // Iterate over grouped items and populate thread panel
  for (const [date, items] of groupedThreadItems) {
    const dateHeader: HTMLElement = document.createElement('div');
    dateHeader.classList.add('threadOnDate');
    dateHeader.textContent = date;
    threadPanel.appendChild(dateHeader);

    const threadsContainer: HTMLElement = document.createElement('div');
    threadsContainer.id = 'threads';
    threadPanel.appendChild(threadsContainer);

    items.forEach(item => {
      const button: HTMLElement = document.createElement('button');
      button.id = item.id;
      button.classList.add('thread', 'w3-button');
      button.onclick = function() {
        loadThread(this.id);
      };

      const div: HTMLElement = document.createElement('div');
      const icon: HTMLElement = document.createElement('i');
      icon.classList.add('threadIcon', 'fa', 'fa-comment');

      div.appendChild(icon);
      div.appendChild(document.createTextNode(item.metadata));
      button.appendChild(div);
      threadsContainer.appendChild(button);
    });
  }
}

function userInputTextAreaGetElement(): HTMLTextAreaElement {
  return document.getElementById("userInput") as HTMLTextAreaElement;
}

function userInputTextAreaInit(): void {
  let inputElement: HTMLTextAreaElement = userInputTextAreaGetElement();
  inputElement.addEventListener("keydown", userInputTextAreaHandleKeyDown());
  inputElement.addEventListener("input", userInputTextAreaUpdateHeight);
}

function userInputTextAreaFocus(): void {
  let inputElement: HTMLTextAreaElement = userInputTextAreaGetElement();
  inputElement.focus();
}

function userInputTextAreaClear(): void {
  let inputElement: HTMLTextAreaElement = userInputTextAreaGetElement();
  inputElement.value = '';
  userInputTextAreaUpdateHeight();
}

function userInputTextAreaUpdateHeight(): void {
  let inputElement: HTMLTextAreaElement = userInputTextAreaGetElement();
  inputElement.style.height = 'auto';
  inputElement.style.height = (inputElement.scrollHeight) + 'px';
}

function userInputTextAreaHandleKeyDown(): (event: KeyboardEvent) => void {
  return function (event: KeyboardEvent): void {
    if (event.key === "Enter") {
      if (!event.shiftKey) {
        event.preventDefault();
        sendMessage();
      }
    }
  };
}

function varsInit(): void {
  document.addEventListener('DOMContentLoaded', varsUpdateHeightsAndWidths);
  window.addEventListener('resize', varsUpdateHeightsAndWidths);
}

function varsUpdateHeightsAndWidths(): void {
  let headerHeight: number = document.querySelector('#header').offsetHeight;
  let userInputHeight: number = document.querySelector('#userInputPanel').offsetHeight;
  document.documentElement.style.setProperty('--header-height', headerHeight + 'px');
  document.documentElement.style.setProperty('--input-height', userInputHeight + 'px');
}

async function newChat(): Promise<void> {
  chatPanelClear();
  logoShow();
  userInputTextAreaFocus();
  await assistantCreateOrRetrieveThread();
}

async function loadThread(threadId: string): Promise<void> {
  chatPanelClear();
  await assistantCreateOrRetrieveThread(threadId);
  userInputTextAreaFocus();
}

function sendMessage(): void {
  let inputElement: HTMLTextAreaElement = userInputTextAreaGetElement();
  let inputValue: string = inputElement.value;

  let notEmpty: boolean = inputValue.trim() !== '';
  if (notEmpty) {
    let html: string | undefined = markdownToHtml(inputValue) || inputValue.replace(/\n/g, '<br/>');
    chatPanelAppendMessage('user', html);
    userInputTextAreaClear();
    varsUpdateHeightsAndWidths();
    assistantProcessInput(inputValue);
  }
}

async function init(): Promise<void> {

  const urlParams: URLSearchParams = new URLSearchParams(window.location.search);

  themeInit();
  markdownInit();
  userInputTextAreaInit();
  varsInit();

  let items: ThreadItem[];
  await assistantInit();

  const fake: boolean = urlParams.get('fake') === 'true';
  if (fake) {
    items = threadItemsLoadFakeData();
    localStorage.setItem('threadItems', JSON.stringify(items));
  }

  const clear: boolean = urlParams.get('clear') === 'true';
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
