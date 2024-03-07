using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Text;
using Microsoft.Azure.WebJobs;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log, ExecutionContext context)
{
    string output;
    try
    {
        var command = "spx recognize --nodefaults @./recognize.job";
        var function = context.FunctionName;
        var directory = context.FunctionDirectory;
        var invocation = context.InvocationId;

        var binDir = Path.Combine(directory, "bin");
        log.LogInformation($"Executing triggered function '{function}' in '{binDir}'");
        log.LogInformation($"Command='{command}' w/ id='{invocation}'");

        var start = new ProcessStartInfo("cmd.exe", $"/c \"{command}\"");
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.WorkingDirectory = binDir;

        var process = Process.Start(start);
        process.WaitForExit();

        output = await process.StandardOutput.ReadToEndAsync();
    }
    catch (Exception ex)
    {
        output = ex.ToString();
        log.LogInformation(output);
    }
    return new OkObjectResult(output);
}