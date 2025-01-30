CONFIG COMMAND

@include.the.config.command

  The `--command` option specifies the configuration data 
  SCOPE of use based on the COMMAND in use.

USAGE: spx config [@FILE] --command COMMAND [...]
   OR: spx config COMMAND [@FILE] [...]

  WHERE: COMMAND is `recognize`
     OR: COMMAND is `synthesize`
     OR: COMMAND is `translate`
     OR: COMMAND is `speaker`
     OR: COMMAND is `profile`
     OR: COMMAND is `batch`
     OR: COMMAND is `csr`
     OR: COMMAND is `webjob`

EXAMPLES

  spx config recognize @default.output --clear
  spx config recognize @default.output --add output.id true
  spx config recognize @default.output --add output.text true

  spx config translate @default.output --clear
  spx config translate @default.output --add output.id true
  spx config translate @default.output --add output.text true
  spx config translate @default.output --add output.translated.text true

  spx recognize --file hello.wav
  spx translate --source en-US --target de --file hello.wav

  spx config synthesize @region --set westus2
  spx config synthesize @key --set 436172626F6E20697320636F6F6C2121

  spx config recognize @region --set eastus
  spx config recognize @key --set 436172626F6E20697320636F6F6C2020

SEE ALSO

  spx help config output
  spx help config scope
  spx help config
