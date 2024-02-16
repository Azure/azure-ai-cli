CONFIG COMMAND

@include.the.config.command

  The `--command` option specifies the configuration data 
  SCOPE of use based on the COMMAND in use.

USAGE: vz config [@FILE] --command COMMAND [...]
   OR: vz config COMMAND [@FILE] [...]

  WHERE: COMMAND is `image`
     OR: COMMAND is `image analyze`
     OR: COMMAND is `image read`
     OR: COMMAND is `face`
     OR: COMMAND is `face enroll`
     OR: COMMAND is `face identify`
     OR: COMMAND is `face verify`

SEE ALSO

  vz help config output
  vz help config scope
  vz help config
