/// <reference types="vite/client" />

interface ImportMetaEnv {
    readonly AZURE_OPENAI_API_KEY: String;
    readonly AZURE_OPENAI_API_VERSION: String;
    readonly AZURE_OPENAI_CHAT_DEPLOYMENT: String;
    readonly AZURE_OPENAI_CHAT_MODEL: String;
    readonly AZURE_OPENAI_EMBEDDING_DEPLOYMENT: String;
    readonly AZURE_OPENAI_EMBEDDING_MODEL: String;
    readonly AZURE_OPENAI_ENDPOINT: String;
    readonly AZURE_OPENAI_EVALUATION_DEPLOYMENT: String;
    readonly AZURE_OPENAI_EVALUATION_MODEL: String;
    readonly AZURE_OPENAI_KEY: String;
    readonly AZURE_OPENAI_ASSISTANT_ID: String;
  }
  
  interface ImportMeta {
    readonly env: ImportMetaEnv
  }