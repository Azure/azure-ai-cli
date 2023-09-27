# Prioritized user needs (Ignite) – Code First  

E2E steel thread spec: specs/simplified-sdk/flows/flows.md 

# Summary 

Committed 

I can easily and quickly install the CLI and SDK with a single command, and it installs quickly with minimal dependencies  

I can get started quickly implementing chat with grounding data by forking/cloning a python repo and running CLI commands in the README 

I can use the AI CLI to build indexes using the sample data provided in the repo 

I can use the AI CLI to do interactive chat using an index as grounding data (project Wednesday from the CLI) 

The sample code in the repo has python code that implements chat protocol interface to do chat with grounding data. It includes a few example implementations: using Azure SDKs directly, using langchain, and using semantic kernel 

I can use the AI CLI on my local compute to run, evaluate, and deploy custom python code that implements the chat protocol interface  

I can use the AI CLI to run, evaluate, and deploy prompt flows, both using local compute and remotely using my project’s compute 

I can use the AI CLI to generate a prompt flow wrapper for Python code and publish the prompt flow to my project where I can do further prompt engineering/evaluation 

I can see my project’s files in VS Code (Web) and use them to work seamlessly with prompt flow across Studio UI and VS Code 

I can evaluate my LLM outputs using evaluation functions in the Azure AI Generative SDK, and integrate evaluation into unit-tests/integration tests by following provided samples 

In scope but at risk: 

Passing connections from project to running promptflows: needs DRI accountable to resolve the details and align across many teams  

I can create/manage datasets from the SDK and CLI: needs resourcing (Sam working with Jose on a proposal) 

I can have my code read default deployment names from environment variables for chat, evaluation, and embedding so that I don’t make typos in deployment names 

I can easily call deployed chat flows with easy to use chat consumption SDKs for: spec is in progress but the runway seems to short, likely to be post-ignite 

I can simulate and evaluate multi-turn chat conversations using the SDK/CLI: net-new functionality, specs coming in hot 

I can visualize metrics from the AOAI fine tuning jobs in the Studio UI: we haven’t had time to come up with a proposal on this yet 

# CF1 

## Core CLI/SDK 

## CF1.1 - I can easily and quickly install the CLI and  SDK with a single command, and it installs quickly with minimal dependencies  

⭐ `ai` CLI DONE ✅ (ubuntu): CF1.1 - I can easily and quickly install the CLI and  SDK with a single command, and it installs quickly with minimal dependencies  
⭐ `ai` CLI DONE ✅ (debian): CF1.1 - I can easily and quickly install the CLI and  SDK with a single command, and it installs quickly with minimal dependencies  
⭐ `ai` CLI TODO ⏹️ (alpine): CF1.1 - I can easily and quickly install the CLI and  SDK with a single command, and it installs quickly with minimal dependencies  
⭐ `ai` CLI DONE ✅ (docker): CF1.1 - I can easily and quickly install the CLI and  SDK with a single command, and it installs quickly with minimal dependencies  
⭐ `ai` CLI TODO ⏹️ (windows): CF1.1 - I can easily and quickly install the CLI and  SDK with a single command, and it installs quickly with minimal dependencies  
⭐ `ai` CLI TODO ⏹️ (macOS): CF1.1 - I can easily and quickly install the CLI and  SDK with a single command, and it installs quickly with minimal dependencies  

A single azure-generative-ai package installs everything developers need to build, evaluate and deploy flows using default options 

Data plane components (chat simulator, conversation simulator) don’t require any control plane calls 

Reduction of the amount of dependencies needed 

PM: Dan 

Dev: Hanchi 

Committed 

## CF1.2 - We have a Generative AI SDK for C# that can be used by the Azure AI CLI 
⭐ `ai` CLI TODO/p2 🔲: CF1.2 - We have a Generative AI SDK for C# that can be used by the Azure AI CLI 

The AI CLI can do everything in C# code without calling into Python 

Specifically, there’s a AI Client and Flows C# API, with identical specs as the Python implementation (modulo language differences) 

Scope: [Gerardo update whats in scope for ignite] 

INTERNAL ONLY release, private preview at best 

PM: Dan 

Dev: Gerardo 

Committed (some portion of it but not all) 

Gerardo to clarify what’s in scope 

## CF1.3 - CLI/SDK can get information from the project without storing additional local metadata 
⭐ `ai` CLI TODO ⏹️: CF1.3 - CLI/SDK can get information from the project without storing additional local metadata

CLI can retrieve more information about connections so that the CLI can take advantage directly (needs region of OpenAI connection) 

CLI/SDK can retrieve the default deployment to use for chat and embeddings 

PM: Dan (+Ankur?) 

Dev: Miles 

At risk – investigating workarounds 

 

Assess impact on UX with Dan/Leah 

## CF1.4 - Telemetry - We have a basic telemetry dashboard for CLI/SDK usage (best effort): calls that were made, success/failure 
⭐ `ai` CLI TODO/p2 🔲: CF1.4 - Telemetry - We have a basic telemetry dashboard for CLI/SDK usage (best effort): calls that were made, success/failure

Cut: Enhanced client-side instrumentation and dashboard will be post-ignite 

PM: Dan 

Dev: Diondra, Ahmet 

Committed 

 

At risk – need spec/investigation to see whats realistic 

 

## CF1.5 - E2E Tests 
⭐ `ai` CLI TODO ⏹️ : CF1.5 - E2E Tests for langchain and SDK - We don’t need to manually test the demo repo every time we update package dependencies 

Our E2E steel thread scenario is running under an automated test (SDK) 

We have an E2E test scenario for langchain and SK 

We don’t need to manually test the demo repo every time we update package dependencies 

PM: Dan 

Dev: Kevin 

 

Committed 

 

## CF1.6 - Canonical L200 getting started sample project for starting with python code using Semantic Kernel 
⭐ `ai` CLI (no work) ⬜: CF1.6 - Canonical L200 getting started sample project for starting with python code using Semantic Kernel

We have a developer friendly sample project that they can use to run through our chat with data hero scenario 

This is a public github repo, the "default getting started” project for starting with code (a replacement/rename of the “aistudio-chat-demo” repo) 

Users can clone the repo, use the CLI to create resources, build index, test/evaluate/deploy the sample chat code (following the E2E spec here) 

The repo includes a sample integration test that uses evaluation, and a github action that deploys the web app to MIR 

Sample test case for the evaluate method 

The sample project should include a template for deploying the backend API to somewhere (Functions, Webapps)? 

PM: Dan (+collab w/ Leah/Arun) 

Dev: David 

Committed 

## CF1.7 - Azure AI samples repo 
⭐ `ai` CLI (no work) ⬜: CF1.7 - Azure AI samples repo

We have an azureai-samples repo that contains SDK samples for different scenarios. 

 Show an example for chat using python sdks directly, langchain, semantic kernel all in the same repo, create separate repos 

Long term it will be cross language, initial goal will be to have python samples 

Open question: how do we differentiate with this repo here: https://github.com/Azure-Samples/azure-ai 

PM: Dan 

Dev: Kevin 

Committed 

## CF1.8 - CLI/SDK Reference Docs 
⭐ `ai` CLI TODO ⏹️ : CF1.8 - CLI/SDK Reference Docs

All of the function calls/inputs/outputs are documented in generated reference docs available 

Where would they be hosted for PrP? (readthedocs style) 

PM: Dan 

Dev: Diondra (SDK), Rob Chambers (CLI) 

Committed for SDK 

 

Open for CLI 

## CF1.9  - Setup and Management 
⭐ `ai` CLI DONE ✅: CF1.9  - Setup and Management
 

Connect to existing resources in project setup 

Select models to deploy at project creation 

Connect to Azure Cognitive Search on project creation or create a new one 

 

PM: Dennis 

Dev: Miles 

Committed 

# CF2 

Setup and Management 

 


## CF2.1 - I can create a project that uses existing resources (in project setup) 
⭐ `ai` CLI TODO ✅!🟧: (need spec/clarity) CF2.1 - I can create a project that uses existing resources (in project setup)
 
Need to simplify this experience so that user just selects resource and new project 

Works already but will need a refresh for the final RP design may be needed 

PM: Dan 

Dev: Miles/Rob 

Open (new) 

## CF2.2 - I can create a new hub/project with all of the resources and models I need 
⭐ `ai` CLI TODO ✅?🟨: (need to revise ??) CF2.2 - I can create a new hub/project with all of the resources and models I need

select models to deploy at AI hub resource creation time 

At resource creation time I can: create a cog search resource and select AOAI models to deploy 

Works already but will need a refresh for final RP design 

PM: Dan 

Dev: Miles/Rob 

Open (new) 

## CF2.3 - I can easily connect my development environment to an existing project 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF2.3 - I can easily connect my development environment to an existing project

Need to simplify this so that user just selects the project 

PM: Dan 

Dev: Miles/Rob 

Open (new) 

# CF3 

Flows 

## CF3.1 - Resolve any naming concerns with “logic app flows” 
⭐ `ai` CLI (no work) ⬜: CF3.1 - Resolve any naming concerns with “logic app flows”

Dan/Nabila work with Kristin on taxonomy 

PM: Dan/Nabila 

UX: Kristin 

Open (not sure what the right next steps are) 

## CF3.2 - I can use the ai cli instead of the pf cli (so that I only need to use one CLI) 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF3.2 - I can use the ai cli instead of the pf cli (so that I only need to use one CLI)

ai supports current capabilities of pf cli: run, test, evaluate,… 

ai is easy to install with a bootstrapper script for installing dependency packages 

User shouldn’t know how we implement it 

PM: Leah 

Dev: Rob Chambers 

Committed 

 

Needs spec 

## CF3.3 - Running a flow locally uses connections from the project 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF3.3 - Running a flow locally uses connections from the project

All promptflow connection types are supported by project connections 

When running in the project context (e.g. with a config.json present) those connections are transferred at runtime from the project 

Connections can be retrieved by importing the promptflow SDK 

For default, well-known connections we set environment variables on the running process 

The project’s default chat, deployment is passed as an environment variable 

stretch: user can override connections locally 

PM: Long (+ Leah) 

Dev: Hanchi + Miles + PF team (Clement) 

Committed 

 

At Risk – needs promptflow to set environment variables 

At Risk – need solution 🟧 from Long (dan confirm with Meng) 

 

Meet with Jie/Lisa to assign a feature crew 

## CF3.4 - I can manage flows via SDK/CLI and transition between code and Studio 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF3.4 - I can manage flows via SDK/CLI and transition between code and Studio


De-prioritized: explicit CRUD support 

(P1: I can “create” a local flow using the AI CLI wrapping PF CLI - tracked in CF3.2) 

P1: I can “publish/upload” a flow to my project [CLI + PF dependency] 

P1: After publishinguploading my flow to my project I can do prompt engineering/evaluation/ deployment from the studio UI [CLI + PF dependency] 

P1: I can download a promptflow from the studio to my local project (and then run it) [Curated Env Image (David) + CF3.3] 

P2: I can list different flows in my project 

Cut: I can distinguish between “Draft” flows that are saved to the studio and published/versioned flows 

PM: Leah 

Dev: David (SDK), Rob (CLI) 

Committed 

 

Need dev spec on aiclient.flows 

 

Dependency: need a service API from PF (workaround: copy paste ugly code) 

 

## CF3.5 - I can more easily wrap my existing python code (custom/langchain/sdk) in a promptflow 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF3.5 - I can more easily wrap my existing python code (custom/langchain/sdk) in a promptflow

There’s a CLI command to generate a promptflow that calls a Python function 

It’s OK if some user “wiring up of code” is needed to connect the promptflow to user code 

Streaming is supported 

If the python code conforms to the chat protocol, it should work out-of-the box 

The generated promptflow should allow user to pass a prompt and parameters from promptflow into the code 

Create a custom python, semantic kernel, and langchain sample 

 

PM: Leah 

Dev: David 

 

Committed 

 

## CF3.6 - The promptflow runtime and vs code share the same default curated environment  
⭐ `ai` CLI (no work) ⬜: CF3.6 - The promptflow runtime and vs code share the same default curated environment

One curated environment has all the packages needed for both 

Open question: if you create a vs code environment does it show up as a runtime? 

PM: Leah (+Who from PF?) 

Dev: David (+who from PF) 

 

Committed 

 

 

## CF3.8 - Stretch: remove the runtime concept from promptflow 
⭐ `ai` CLI (no work) ⬜: CF3.8 - Stretch: remove the runtime concept from promptflow

Add ability for users to specify a “default” or specific curated environment to use in the promptflow .dag.yaml 

Then users just pick compute they want to run on (compute instance, serverless) and the environment is created for them on demand 

We have a list of “running environments” somewhere so users can clean them up (e.g. if their compute instance is overloaded) 

PM: Leah? 

Dev: someone on the PF team? 

 

Rob Young was looking at serverless aspects 

Cut 

 

Needs PM support, do this after Ignite 

# CF4 - Data & Indexing 

## CF4.1  - I can do dataset CRUD using the AI CLI/SDK 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF4.1  - I can do dataset CRUD using the AI CLI/SDK

The experience is optimized for creating a dataset from local files, cloud storage URI, and web URL 

Connections can be automatically created as part of the data create experience 

I can consume datasets using pandas with an azureml URI or by mounting my projects data on my local file system 

PM: Xun Wang Dev: request Jose’s team help 

Open 

 

At Risk, needs resourcing (Sam following up with Jose) 

## CF4.2 - I can build MLIndex with minimal local dependencies using the CLI/SDK 
⭐ `ai` CLI TODO ⏹️!🟨: (need spec/clarity)  CF4.2 - I can build MLIndex with minimal local dependencies using the CLI/SDK

Default installation of the Generative AI SDK includes ability to create indexes using cloud compute (project’s serverless compute or cog search directly) 

Users can optionally install generative-ai[index] if they want the ability to build indexes using local compute 

PM: Sam Kemp 

Dev: Neehar (Exp) + Lucas (Data) 

Committed 

 

PR open to move rag modules into the SDK 

## CF4.3 - Project Weds: pre-processing API built on unified platform 
⭐ `ai` CLI TODO ✅!🟧: (need spec/clarity)  CF4.3 - Project Weds: pre-processing API built on unified platform

Decide which team owns which parts of the API/Stack for creating and connecting to indexes 

Ensure we can support all the popular data sources and index types (pinecone, FAISS, postgresql, cosmosdb) 

Define the API shape for preprocessing API 

Capture “Wednesday” as a flow for evaluation and deployment 

We want to leverage common code when using cog search as an index 

PM: Sam Kemp + Arthy  

Dev: Kayla Ames, Kevin Endres, Jose Caldaza 

Probably cut (but keep making progress) 

 

At risk – convergence has not started yet (need to clarify impact) 

## CF4.4 - I can build indexes using Fabric as a datasource 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF4.4 - I can build indexes using Fabric as a datasource

I can use a registered dataset or Fabric URI as input to build_mlindex 

PM: Sam 

Dev: Neehar 

Open – Sam to close on the right approach 

## CF4.5 - I can build an index in the cloud using project compute 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF4.5 - I can build an index in the cloud using project compute

Need to review/update the existing implementation of build_mlindex_in_cloud 

PM: Bala 

Dev: Neehar 

Committed 

## CF4.6 - Stretch: I can configure datasets/indexes to refresh periodically with latest data 
⭐ `ai` CLI TODO ⏹️ ◼️: CF4.6 - Stretch: I can configure datasets/indexes to refresh periodically with latest data

Datasets can be created that connect to external data, import & refresh on a schedule 

Indexes can be configured to use those data sets, and automatically be refreshed  

Open question: should this move to cog search backend instead? 

PM: Sam Kemp 

Dev: request area team help 

Cut 

## CF4.7 - Stretch: Project Wednesday: inferencing API leverages Azure AI concepts 
⭐ `ai` CLI DONE ✅?🟨: (need to revise ??) CF4.7 - Stretch: Project Wednesday: inferencing API leverages Azure AI concepts

Inference on a “Wednesday” flow 

PM: 

Dev: Kayla Ames, Kevin Endres 

 

Cut 

# CF5 - Evaluation 

## CF5.1 - Prompts are logged to the evaluation result when they are passed as input parameters 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF5.1 - Prompts are logged to the evaluation result when they are passed as input parameters

I can run an evaluation that uses N different prompt templates, and find which template had the best results using the Studio UI 

Dependency: evaluation UI shows prompt parameters 

PM: Minsoo 

Dev: Hanchi 

Done (integrate this into the demo!) 

## CF5.2 - Users can run our built-in evaluations on local flows with the default SDK/CLI installation 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF5.2 - Users can run our built-in evaluations on local flows with the default SDK/CLI installation

Polish the evaluate() function interface. 

Users can calculate GPT assisted metrics with the default install of the generative AI SDK 

We should support “bulk evaluation” on an input dataset as well as evaluate a single input/output scenario 

To save on cost of generating outputs, we should also enable developers to evaluate just output dataset 

Users should not need to install or directly call the azureml-metrics package 

If they want to do metrics calculation locally, then they need to install the azure-generative-ai[evaluation] extra 

Open question: can we reduce the dependencies such that local metrics calculation is included 

PM: Minsoo 

Dev: Hanchi 

Committed 

 

Need spec? 

 

Its all there we just need to decide how to factor it 

 

Finalize on the evaluate function interface 

 

## CF5.3 - Conversation simulator 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF5.3 - Conversation simulator

I can use conversation generator with my local chat function/flow to simulate a conversation as input to evaluation 

Clean up the LLM interface code using built-in entities 

PM: Minsoo 

Dev: Ke 

Committed 

## CF5.4 - Dataset generator 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF5.4 - Dataset generator

Clean up the LLM interface code using built-in entities 

PM: Bala 

Dev: Prakhar 

Committed 

## CF5.5 - Users can ingest evaluation outputs directly from CLI/SDK outputs so that they can use evaluation in their test/automation code  
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF5.5 - Users can ingest evaluation outputs directly from CLI/SDK outputs so that they can use evaluation in their test/automation code

We have an example of how to do evaluation as part of an integration test 

Evaluation can be configured to print out or save to json file the results of a local evaluation 

You can configure the results to display just the flow outputs, just the evaluation metrics, or both 

PM: Minsoo 

Dev: Hanchi 

Committed 

 

Needs detailed requirements, but should be straight forward) 

## CF5.6 - Stretch: Users can run an evaluation remotely 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF5.6 - Stretch: Users can run an evaluation remotely

Using the –remote flag (CLI) or I can run an evaluation remotely 

Users can run a remote evaluation without installing additional dependencies 

 

PM: Minsoo 

Dev: Hanchi 

 

Stretch goal (not required for ignite) 

 

## CF5.7 - Out of scope: custom evaluators from the SDK (use promptflow?) 
⭐ `ai` CLI TODO ⏹️ ◼️: CF5.7 - Out of scope: custom evaluators from the SDK (use promptflow?)

We should think about our design in parallel so we work towards the right solution 

If custom evaluators are implementing using promptflow, the current mlflow context is passed to the running code 

 

PM: Minsoo 

Cut 

# CF6 - Deployment 

## CF6.1 - I can deploy promptflows to the project (MIR), including ones that wrap langchain/SK, using AI CLI/SDK 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF6.1 - I can deploy promptflows to the project (MIR), including ones that wrap langchain/SK, using AI CLI/SDK

ai deploy supports deploying promptflows 

secrets are automatically set up on the container 

PM: Sanghee 

Dev: Neehar 

Committed 

## CF6.2 - I can consume a deployed promptflow using the chat flow SDK 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF6.2 - I can consume a deployed promptflow using the chat flow SDK

I can call a deployed promptflow/chat function using a small set of cross-language SDKs 

TBD: Consume tab has a simpler set of SDKs for calling into “chat flows” 

TBD: Ideally all endpoints in a project use the same key (so that auth is simple) 

PM: DanielS/Rohit 

Dev: Rob Chambers 

Off Track – not funded 

## CF6.3 - Improve experience of deploying models from the model catalog  
⭐ `ai` CLI TODO ⏹️ ◼️: CF6.3 - Improve experience of deploying models from the model catalog

Refer to models by name/id instead of by registry 

Make it easy to pick the sku to use from the CLI/SDK (suggest ones with available quota) 

Content safety is turned on by default for OSS model deployments 

PM: Shané 

Dev: Neehar 

Committed 

## CF6.4 - I can set the default chat and embedding deployments from a CLI/SDK call 
⭐ `ai` CLI DONE ✅?🟨: CF6.4 - I can set the default chat and embedding deployments from a CLI/SDK call

Dependency: Ffrom the studio I can set a deployment as the default chat deployment or embedding deployment (e.g. select a deployment and in the toolbar click “Set Default -> Chat, Embedding”) 

I can make a CLI/SDK call to set the default chat and embedding deployment 

Open question: do we need to handle the case where these come from different  

PM: Sanghee 

Dev: Neehar 

Open 

 

Risk: totally new, need detailed spec/owner 

 

## CF6.5 - Generate a docker file and/or bicep template (stretch) that can be used to deploy the flow into production 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF6.5 - Generate a docker file and/or bicep template (stretch) that can be used to deploy the flow into production

Using the SDK I can generate a Dockerfile for the flow 

Stretch: Using the SDK I can generate a deployment template for the flow 

Stretch: We could offer an option for azure container apps or azure functions 

PM: Dan/Arun 

Dev: Neehar 

Committed 

## CF6.6 - Can enable content safety filter on OSS model deployment 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF6.6 - Can enable content safety filter on OSS model deployment

Content safety filter is enabled on OSS models during deployment (via wizard) with appropriate secret injection for security/compliance (link). 

PM: Sanghee 

Dev: ?  

Open 

## CF6.7 - Can enable default monitor (MDC+default GSQ template) during flow deployment creation 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF6.7 - Can enable default monitor (MDC+default GSQ template) during flow deployment creation

Enable default monitor as part of flow deployment 

PM: Will, Sanghee 

Dev: ? 

Open 

# CF7 - Fine Tuning 

## CF7.1 - When fine tuning OAI models in my AI project using the OpenAI SDK, the fine tuning metrics are logged /visible to the project 
⭐ `ai` CLI TODO ⏹️!🟧: (need spec/clarity)  CF7.1 - When fine tuning OAI models in my AI project using the OpenAI SDK, the fine tuning metrics are logged /visible to the project

The OpenAI SDK is able to know that its running in the context of a project 

Fine tuning metrics automatically logged to the projects run-history 

After the fine tune run is completed, user can view the models in a “models” page on the studio UI 

metrics are displayed in the Studio UI (propose: put them as a details page on the model) 

The OpenAI SDK can consume a registered dataset when fine-tuning a model 

PM: Shané 

Dev: Rob Chambers 

CUT 

 

Follow up discusión on requirements 

# CF8 - VS Code 

## CF8.1 - Folder structure 
⭐ `ai` CLI (no work) ⬜: CF8.1 - Folder structure

I can access shared project files, including my promptflows, from VS Code 

VS Code opens in the project  folder 

Stretch/tbd: ~ takes you to project folder instead of VSCode/home  (look into symlinks?) 

PM: Leah 

Dev: Serkan/Ada/Isis 

Committed 

## CF8.2 - Custom environments 
⭐ `ai` CLI (no work) ⬜: CF8.2 - Custom environments

I can look at the default environment details and choose to build off the default environment (link to Azure ML from Azure AI) 

I can create a new custom environment (in Azure ML) 

I can modify the container as I am working and those changes will be persisted between sessions 

[has risky dependencies] I can choose from custom environments to launch VS Code, and the UI tells me that I may need to install dependencies for CLI/SDK 

CUT: I can save that environment or updates to the environment with the project, or if I have access to write to the hub, promote it to a hub-level shared object that can be reused across projects 

PM: Leah 

Dev: Serkan/Ada/Isis 

Committed 

 

Need to confirm with Serkan that he can do the work to add Custom environments  
 

Dependency: CI/custom app team needs to allow picking custom environments in the custom app definition – need to Create work item and align with team @Leah 

## CF8.3 - Stretch: I can add repos to my project so that VS Code can clone/open those repos directly 
⭐ `ai` CLI (no work) ⬜: CF8.3 - Stretch: I can add repos to my project so that VS Code can clone/open those repos directly

I can add repos to my project  

I can clone those repos to VS Code 

If a .devcontainer, requirements.txt or environment.yml exists at the root of the repo, then that is used to automatically build/start the environment 

(how is auth handled, does each user need to bring their own auth?) 

PM: Leah 

Dev: Serkan? 

UX: Will 

 

Cut 

 

