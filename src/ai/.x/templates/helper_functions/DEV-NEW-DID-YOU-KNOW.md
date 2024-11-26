`#e0;DID YOU KNOW?`

  Next time, you can supply additional instructions to modify the
  helper-functions project source code to include a function of
  your own, created from a simple prompt.

  USAGE: ai dev new [...] --instructions INSTRUCTIONS
     OR: ai dev new [...] --instructions @FILE

    WHERE: INSTRUCTIONS are the instructions
       OR: FILE is the name of a file containing instructions
  
  EXAMPLE:

    ai dev new helper-functions --csharp --instructions @instructions.txt

    WHERE: instructions.txt file contains text like this:

      Create a helper function that can run any CLI process:
      - Parameter 1 should be the process executable file name
      - Parameter 2 should be arguments to be passed to the process
      - Function should return the combination of stdout and stderr
      - If there's an exception, return the exception message instead