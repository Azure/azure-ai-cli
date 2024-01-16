# `spx` CLI Yaml Test Adapter + Test Runner

PRE-REQUISITES:
* `spx` must be accessible in `PATH`
* `spx` must be configured as required for tests (e.g. `region` and `key`, or `endpoint` setup)
- see: https://crbn.us/searchdocs?spx
- OR ...
  ```dotnetcli
  dotnet tool install --global Microsoft.CognitiveServices.Speech.CLI
  spx config @region --set YOUR-REGION-HERE
  spx config @key --set YOUR-KEY-HERE`
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
  cd src\TestRunner\bin\Debug\net6.0
  dotnet test Azure.AI.CLI.TestRunner.dll --logger:trx
  ```
* RELEASE:
  ```dotnetcli
  cd src\TestRunner\bin\Release\net6.0
  dotnet test Azure.AI.CLI.TestRunner.dll --logger:trx --logger:console;verbosity=normal
  ```

**dotnet vstest**  
OR ... [Build](#BUILD) first, then w/CLI:
* DEBUG:
  ```dotnetcli
  cd src\TestRunner\bin\Debug\net6.0
  dotnet vstest Azure.AI.CLI.TestRunner.dll --logger:trx
  ```
* RELEASE:
  ```dotnetcli
  cd src\TestRunner\bin\Release\net6.0
  dotnet vstest Azure.AI.CLI.TestRunner.dll --logger:trx --logger:console;verbosity=normal
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
  cd src\TestRunner\bin\Debug\net6.0
  dotnet test Azure.AI.CLI.TestRunner.dll -t
  ```
* RELEASE:
  ```dotnetcli
  cd src\TestRunner\bin\Release\net6.0
  dotnet test Azure.AI.CLI.TestRunner.dll -t
  ```

**dotnet vstest**  
OR ... [Build](#BUILD) first, then w/CLI:
* DEBUG:
  ```dotnetcli
  cd src\TestRunner\bin\Debug\net6.0
  dotnet vstest Azure.AI.CLI.TestRunner.dll -lt
  ```
* RELEASE:
  ```dotnetcli
  cd src\TestRunner\bin\Release\net6.0
  dotnet vstest Azure.AI.CLI.TestRunner.dll -lt
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
  cd src\TestRunner\bin\Debug\net6.0
  dotnet test --filter:name~PARTIAL_NAME Azure.AI.CLI.TestRunner.dll
  ```
* RELEASE:
  ```dotnetcli
  cd src\TestRunner\bin\Release\net6.0
  dotnet test --filter:name~PARTIAL_NAME Azure.AI.CLI.TestRunner.dll
  ```

**dotnet vstest**  
OR ... [Build](#BUILD) first, then w/CLI:
* DEBUG:
  ```dotnetcli
  cd src\TestRunner\bin\Debug\net6.0
  dotnet vstest Azure.AI.CLI.TestRunner.dll --logger:trx --testcasefilter:name~PARTIAL_NAME
  ```
* RELEASE:
  ```dotnetcli
  cd src\TestRunner\bin\Release\net6.0
  dotnet vstest Azure.AI.CLI.TestRunner.dll --logger:trx --testcasefilter:name~PARTIAL_NAME
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
* Open `SpxYaml.sln`
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

e.g. `dotnet vstest Azure.AI.CLI.TestRunner.dll --logger:trx --logger:console;verbosity=normal`
