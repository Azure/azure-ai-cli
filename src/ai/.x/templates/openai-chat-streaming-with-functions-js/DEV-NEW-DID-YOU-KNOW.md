{{if contains(toupper("{OPENAI_CLOUD}"), "AZURE")}}
`#e0;DID YOU KNOW?`

  Next time, you can specify the OpenAI cloud to use.

  USAGE: ai dev new [...] --var OPENAI_CLOUD=Azure (default)
     OR: ai dev new [...] --var OPENAI_CLOUD=OpenAI
{{endif}}