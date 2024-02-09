# `ai` CLI Yaml Test Adapter

PRE-REQUISITES:
* `ai` must be accessible in `PATH`
* `ai` must be configured as required for tests (run `ai init`, or use `ai config --set KEY=VALUE` for all required information)
- see: https://crbn.us/searchdocs?ai
- OR ...
  ```dotnetcli
  dotnet tool install --global Azure.AI.CLI
  ai init
  ```

## Run ALL tests

**dotnet test**  
From fresh clone (one step, CLI):
* DEBUG:
  ```dotnetcli
  dotnet test --logger:trx
  ```
* RELEASE:
  ```dotnetcli
  dotnet test --configuration release --logger:trx
  ```

OR ... [Build](#BUILD) first, then w/CLI:
* DEBUG:
  ```dotnetcli
  cd tests\testadapter\bin\Debug\net8.0
  dotnet test Azure.AI.CLI.TestAdapter.dll --logger:trx
  ```
* RELEASE:
  ```dotnetcli
  cd tests\testadapter\bin\Release\net8.0
  dotnet test Azure.AI.CLI.TestAdapter.dll --logger:trx --logger:console;verbosity=normal
  ```

**dotnet vstest**  
OR ... [Build](#BUILD) first, then w/CLI:
* DEBUG:
  ```dotnetcli
  cd tests\testadapter\bin\Debug\net8.0
  dotnet vstest Azure.AI.CLI.TestAdapter.dll --logger:trx
  ```
* RELEASE:
  ```dotnetcli
  cd tests\testadapter\bin\Release\net8.0
  dotnet vstest Azure.AI.CLI.TestAdapter.dll --logger:trx --logger:console;verbosity=normal
  ```

**VS 2019+**  
OR ... [Build](#BUILD) first, then w/Visual Studio 2019+:
* Open Test Explorer (`<ctrl-E>T`)
* Run all tests (`<ctrl-R>V`)

---
## LIST tests

**dotnet test**  
From fresh clone (one step, CLI):
* DEBUG:
  ```dotnetcli
  dotnet test -t
  ```
* RELEASE:
  ```dotnetcli
  dotnet test --configuration release -t
  ```

OR ... [Build](#BUILD) first, then w/CLI:
* DEBUG:
  ```dotnetcli
  cd tests\testadapter\bin\Debug\net8.0
  dotnet test Azure.AI.CLI.TestAdapter.dll -t
  ```
* RELEASE:
  ```dotnetcli
  cd tests\testadapter\bin\Release\net8.0
  dotnet test Azure.AI.CLI.TestAdapter.dll -t
  ```

**dotnet vstest**  
OR ... [Build](#BUILD) first, then w/CLI:
* DEBUG:
  ```dotnetcli
  cd tests\testadapter\bin\Debug\net8.0
  dotnet vstest Azure.AI.CLI.TestAdapter.dll -lt
  ```
* RELEASE:
  ```dotnetcli
  cd tests\testadapter\bin\Release\net8.0
  dotnet vstest Azure.AI.CLI.TestAdapter.dll -lt
  ```

---
## Run SOME tests

**dotnet test**  
From fresh clone (one step, CLI):
* DEBUG:
  ```dotnetcli
  dotnet test --filter:name~PARTIAL_NAME
  ```
* RELEASE:
  ```dotnetcli
  dotnet test --configuration release --filter:name~PARTIAL_NAME
  ```

OR ... [Build](#BUILD) first, then w/CLI:

* DEBUG:
  ```dotnetcli
  cd tests\testadapter\bin\Debug\net8.0
  dotnet test --filter:name~PARTIAL_NAME Azure.AI.CLI.TestAdapter.dll
  ```
* RELEASE:
  ```dotnetcli
  cd tests\testadapter\bin\Release\net8.0
  dotnet test --filter:name~PARTIAL_NAME Azure.AI.CLI.TestAdapter.dll
  ```

**dotnet vstest**  
OR ... [Build](#BUILD) first, then w/CLI:
* DEBUG:
  ```dotnetcli
  cd tests\testadapter\bin\Debug\net8.0
  dotnet vstest Azure.AI.CLI.TestAdapter.dll --logger:trx --testcasefilter:name~PARTIAL_NAME
  ```
* RELEASE:
  ```dotnetcli
  cd tests\testadapter\bin\Release\net8.0
  dotnet vstest Azure.AI.CLI.TestAdapter.dll --logger:trx --testcasefilter:name~PARTIAL_NAME
  ```

**VS 2019+**  
OR ... [Build](#BUILD) first, then w/Visual Studio 2019+:
* Open Test Explorer (`<ctrl-E>T`)
- Select tests (w/ mouse: `Left-click`, extend w/`Shift-left-click` and/or `Ctrl-left-click`)
- OR ... `<ctrl-E>`, enter search criteria, press `<ENTER>`
* Run selected tests (w/ mouse: `Right-click`, click on `Run`)

**Additional CLI test case filters**

`<property>Operator<value>[|&<Expression>]`

Where Operator is one of `=`, `!=` or `~` (Operator ~ has 'contains'
semantics and is applicable for string properties like DisplayName).

Parenthesis () can be used to group sub-expressions.

| property | aliases | example |
|-|-|-|
| Name | DisplayName | `Name=NAME`
| | | `Name!=NAME`
| | | `Name~PARTIAL`
| fqn | FullyQualifiedName | `fqn=yaml.FILE.AREA.CLASS.NAME`
| | | `fqn!=yaml.FILE.AREA.CLASS.NAME`
| | | `fqn~PARTIAL`
| command | | `command~recognize`
| | | `command~synthesize`
| | | `command~translate`
| | | `command~weather`
| | | `command~mp3`
| script | | `script~echo`
| | | `script~recognize`
| | | `script~weather`
| | | `script~mp3`
| expect | | `expect~RECOGNIZED:`
| not-expect | | `not-expect~ERROR`
| log-expect | | `log-expect~path:`
| log-not-expect | | `log-not-expect~ERROR`

---
# BUILD

**dotnet build**
* DEBUG: `dotnet build`
* RELEASE: `dotnet build --configuration release` 

**VS 2019+**
* Open `ai-cli.sln`
* Select `Debug` or `Release`
* Run (`<ctrl-shift-B>`)

---

## ADDITIONAL OPTIONS

**dotnet test**
Console logging: `-v` or `--verbosity` followed one of:
* `q[uiet]`
* `m[inimal]`
* `n[ormal]`
* `d[etailed]`
* `diag[nostic]`

e.g. `dotnet test --configuration release --v n`

**dotnet vstest**
Console logging: `--logger:console`, optionally followed by one of:
* `;verbosity=quiet`
* `;verbosity=minimal`
* `;verbosity=normal`
* `;verbosity=detailed`

e.g. `dotnet vstest Azure.AI.CLI.TestAdapter.dll --logger:trx --logger:console;verbosity=normal`
