param(
    [string]$organization,
    [string]$project,
    [string]$pat,
    [string]$workItemId,
    [string]$newState
)

# Map the GitHub issue state to an ADO work item state
$adoState = if ($githubIssue.state -eq "open") { "New" }
elseif ($githubIssue.state -eq "closed") { "Done" }
else {
    Write-Host "Unknown GitHub issue state: $($githubIssue.state)"
    exit 0
}

$base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$($pat)"))
$headers = @{Authorization=("Basic {0}" -f $base64AuthInfo)}

$body = @{
    id = $adoWorkItemId
    fields = @{
        "System.State" = $adoState
    }
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://dev.azure.com/$organization/$project/_apis/wit/workitems/$workItemId?api-version=6.0" -Method Patch -Body $body -ContentType "application/json-patch+json" -Headers $adoHeaders
