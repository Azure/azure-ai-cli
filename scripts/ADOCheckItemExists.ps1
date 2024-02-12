# check-ado-item.ps1

param(
    [string]$organization,
    [string]$project,
    [string]$pat,
    [string]$title,
    [string]$areaPath
)

$base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$($pat)"))
$headers = @{Authorization=("Basic {0}" -f $base64AuthInfo)}

$query = @"
SELECT [System.Id]
FROM workitems
WHERE [System.Title] = '$title'
AND [System.AreaPath] = '$areaPath'
"@

$body = @{
    query = $query
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://dev.azure.com/$organization/$project/_apis/wit/wiql?api-version=6.0" -Method Post -Body $body -ContentType "application/json" -Headers $headers

if ($response.workItems.Count -gt 0) {
    $true
} else {
    $false
}
