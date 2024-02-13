param(
    [string]$organization,
    [string]$project,
    [string]$pat,
    [string]$workItemId,
    [string]$newState
)

# Map the GitHub issue state to an ADO work item state
$adoState = if ($newState -eq "open") { "New" }
elseif ($newState -eq "closed") { "Done" }
else {
    Write-Host "Unknown GitHub issue state: $($newState)"
    exit 0
}

$B64Pat = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes("`:$pat"))
$headers = @{
    "Authorization" = "Basic $B64Pat"
}

$body = @(
   [ordered] @{  op = 'add';  path = '/fields/System.State';  value = "$adoState"  }
)
# Convert the body to JSON
$bodyJson = ConvertTo-Json -InputObject $body

Invoke-RestMethod -Uri "https://dev.azure.com/$organization/$project/_apis/wit/workitems/$($workItemId)?api-version=7.1" -Method Patch -Body $bodyJson -ContentType "application/json-patch+json" -Headers $headers
