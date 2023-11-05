|     | `--question`          | `--replace`|  runs | uses | must exist |
|--|--|--|--|--|--|
|     | `"... {lang1} ..."`   | `--replace lang1 "C#"`  | 1 time | item
| new | `"... {lang2} ..."`   | `--replace-foreach lang2 in "Python;C#;JavaScript"`  | 3 times | items between `';'`
|     |  |  |
|     | `"... {file1} ..."`   | `--replace file1 @Program.cs`  | 1 time | **content of file** | YES
| new | `"... {@file2} ..."`  | `--replace file2 Program.cs`  | 1 time | **content of file** | YES
|     |  |  |
| new | `"... {file3} ..."`   | `--replace-foreach file3 in files "*command.cs"`  | N times | **name of file** | YES
|     |  |  |
| new | `"... {@file4} ..."`  | `--replace-foreach file4 in files "*command.cs"`  | N times | **content of file** | YES

