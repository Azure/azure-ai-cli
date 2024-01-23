# new ai-init state machine
(derived from Leah's spec)

## `ai init` commands

`ai init` => [init-root](#init-root)

`ai init standalone` => [init-root-standalone-select-or-create](#init-root-standalone-select-or-create)

**project**:  
`ai init project` =>  
1. [subscription-select](#subscription-select)
1. [project-create-or-select](#project-create-or-select)  

`ai init project new` =>
1. [subscription-select](#subscription-select)
1. [project-create](#project-create)  

`ai init project select`
1. [subscription-select](#subscription-select)
1. => [project-select](#project-select)  

**openai**:  
`ai init openai` =>
1. [subscription-select](#subscription-select)
1. [openai-create-or-select](#openai-create-or-select)  

`ai init openai new` =>
1. [subscription-select](#subscription-select)
1. [openai-create](#openai-create)  

`ai init openai select` =>
1. [subscription-select](#subscription-select)
1. [openai-select](#openai-select)

**search**:  
`ai init search` =>
1. [subscription-select](#subscription-select)
1. [search-create-or-select](#search-create-or-select)  

`ai init search new` =>
1. [subscription-select](#subscription-select)
1. [search-create](#search-create)  

`ai init search select` =>
1. [subscription-select](#subscription-select)
1. [search-select](#search-select)  

**speech**:  
`ai init speech` =>
1. [subscription-select](#subscription-select)
1. [speech-create-or-select](#speech-create-or-select)  

`ai init speech new` =>
1. [subscription-select](#subscription-select)
1. [speech-create](#speech-create)  

`ai init speech select` =>
1. [subscription-select](#subscription-select)
1. [speech-select](#speech-select)  

## init-root
- if not exist config.json => [init-root-menu-pick](#init-root-menu-pick)
- if exist config.json => init-root-verify-config:
  * if config good => [init-root-confirm-verified-config](#init-root-confirm-verified-config)
  * if config bad => [init-root-menu-pick](#init-root-menu-pick)

## init-root-confirm-verified-config

▤ ↕ ⎆
- "Use this project..." => init-root-finish-print-verified-config 🛑
- "Init diff resources..." => [init-root-menu-pick](#init-root-menu-pick)

## init-root-menu-pick

▤ ↕ ⎆
- "a new AI Project" => [init-root-project-create](#init-root-project-create)
- "an existing AI Project" => [init-root-project-select](#init-root-project-select)
- "standalone resources" => [init-root-standalone-select-or-create](#init-root-standalone-select-or-create)

## init-root-project-create

1. [project-name-new-get](#project-name-new-get)
1. [subscription-select](#subscription-select)
1. [hub-select-or-create](#hub-select-or-create)
1. [openai-skip-create-or-select](#openai-skip-create-or-select)  
1. [search-skip-create-or-select](#search-skip-create-or-select)  
1. [speech-skip-create-or-select](#speech-skip-create-or-select)  
1. [project-create](#project-create)
1. init-root-finish-print-verified-config 🛑

## init-root-project-select

1. [subscription-select](#subscription-select)
1. [hub-select](#hub-select)
1. [project-select](#project-select)
1. [standalone-select-or-create](#standalone-select-or-create)
1. init-root-finish-print-verified-config 🛑

## init-root-standalone-select-or-create

- *"Azure OpenAI"* => [init-root-openai-create-or-select](#init-root-openai-create-or-select)
- *"Azure OpenAI Deployment (Chat)"* => [init-root-openai-deployment-chat-create-or-select](#init-root-openai-deployment-chat-create-or-select)
- *"Azure OpenAI Deployment (Embedding)"* => [init-root-openai-deployment-embedding-create-or-select](#init-root-openai-deployment-embedding-create-or-select)
- *"Azure OpenAI Deployment (Evaluation)"* => [init-root-openai-deployment-evaluation-create-or-select](#init-root-openai-deployment-evaluation-create-or-select)
- *"Azure Search"* => [init-root-search-create-or-select](#init-root-search-create-or-select)
- *"Azure Speech Service"* => [init-root-speech-create-or-select](#init-root-speech-create-or-select)

And then optionally => [standalone-done-select-or-create](#standalone-done-select-or-create) 

## standalone-select-or-create

- *"Azure OpenAI"* => [openai-create-or-select](#openai-create-or-select)
- *"Azure OpenAI Deployment (Chat)"* => [openai-deployment-chat-create-or-select](#openai-deployment-chat-create-or-select)
- *"Azure OpenAI Deployment (Embedding)"* => [openai-deployment-embedding-create-or-select](#openai-deployment-embedding-create-or-select)
- *"Azure OpenAI Deployment (Evaluation)"* => [openai-deployment-evaluation-create-or-select](#openai-deployment-evaluation-create-or-select)
- *"Azure Search"* => [search-create-or-select](#search-create-or-select)
- *"Azure Speech Service"* => [speech-create-or-select](#speech-create-or-select)

And then optionally => [standalone-done-select-or-create](#standalone-done-select-or-create)

## standalone-done-select-or-create
- *"(Done)"* => 🛑
- *"Azure OpenAI"* => [openai-create-or-select](#openai-create-or-select)
- *"Azure OpenAI Deployment (Chat)"* => [openai-deployment-chat-create-or-select](#openai-deployment-chat-create-or-select)
- *"Azure OpenAI Deployment (Embedding)"* => [openai-deployment-embedding-create-or-select](#openai-deployment-embedding-create-or-select)
- *"Azure OpenAI Deployment (Evaluation)"* => [openai-deployment-evaluation-create-or-select](#openai-deployment-evaluation-create-or-select)
- *"Azure Search"* => [search-create-or-select](#search-create-or-select)
- *"Azure Speech Service"* => [speech-create-or-select](#speech-create-or-select)

## init-root-openai-create-or-select

1. [subscription-select](#subscription-select)
1. [openai-create-or-select](#openai-create-or-select)

## init-root-openai-deployment-chat-create-or-select

1. [subscription-select](#subscription-select)
1. [openai-deployment-chat-create-or-select](#openai-deployment-chat-create-or-select)

## init-root-openai-deployment-embedding-create-or-select

1. [subscription-select](#subscription-select)
1. [openai-deployment-embedding-create-or-select](#openai-deployment-embedding-create-or-select)

## init-root-openai-deployment-evaluation-create-or-select

1. [subscription-select](#subscription-select)
1. [openai-deployment-evaluation-create-or-select](#openai-deployment-evaluation-create-or-select)

## init-root-search-create-or-select

1. [subscription-select](#subscription-select)
1. [search-create-or-select](#search-create-or-select)

## init-root-speech-create-or-select

1. [subscription-select](#subscription-select)
1. [speech-create-or-select](#speech-create-or-select)

## openai-skip-create-or-select

NOTE: Added a skip option to top of "create-or-select" flow

(existing UX flow)

## search-skip-create-or-select

NOTE: Added a skip option to top of "create-or-select" flow

(existing UX flow)

## speech-skip-create-or-select

NOTE: Added a skip option to top of "create-or-select" flow

(existing UX flow)

## subscription-select

(existing UX flow)

## project-create-or-select

(existing UX flow)

## project-create

(existing UX flow)

## project-select

(existing UX flow)

## hub-select-or-create

(existing UX flow)

## hub-select

(existing UX flow)

## openai-create-or-select

(existing UX flow)

## openai-create

(existing UX flow)

## openai-select

(existing UX flow)

## openai-deployment-chat-create-or-select

(existing UX flow)

## openai-deployment-chat-create

(existing UX flow)

## openai-deployment-chat-select

(existing UX flow)

## openai-deployment-embedding-create-or-select

(existing UX flow)

## openai-deployment-embedding-create

(existing UX flow)

## openai-deployment-embedding-select

(existing UX flow)

## openai-deployment-evaluation-create-or-select

(existing UX flow)

## openai-deployment-evaluation-create

(existing UX flow)

## openai-deployment-evaluation-select

(existing UX flow)

## search-create-or-select

(existing UX flow)

## search-create

(existing UX flow)

## search-select

(existing UX flow)

## speech-create-or-select

(existing UX flow)

## speech-create

(existing UX flow)

## speech-select

(existing UX flow)

## project-name-new-get

(existing UX flow)

## hub-select-or-create

(existing UX flow)

