# Define the organization, project, and personal access token
param (
    [Parameter(Mandatory=$true)]
    [string]$pat,
    [Parameter(Mandatory=$true)]
    [string]$title,
    [Parameter(Mandatory=$true)]
    [string]$description,
    [Parameter(Mandatory=$true)]
    [string]$organization,
    [Parameter(Mandatory=$true)]
    [string]$project,
    [Parameter(Mandatory=$true)]
    [string]$workItemType,
    [Parameter(Mandatory=$true)]
    [string]$iterationPath,
    [Parameter(Mandatory=$true)]
    [string]$areaPath
)

# Define the URL for the REST API call
$url = "https://dev.azure.com/$organization/$project/_apis/wit/workitems/`$$workItemType?api-version=6.0"

# Define the body of the REST API call
$body = @"
[
    {
        "op": "add",
        "path": "/fields/System.Title",
        "value": "$title"
    },
    {
        "op": "add",
        "path": "/fields/System.Description",
        "value": "$description"
    },
    {
        "op": "add",
        "path": "/fields/System.IterationPath",
        "value": "$iterationPath"
    },
    {
        "op": "add",
        "path": "/fields/System.AreaPath",
        "value": "$areaPath"
    }
]
"@

# Convert the body to JSON
$bodyJson = $body | ConvertTo-Json

# Define the headers for the REST API call
$headers = @{
    "Content-Type" = "application/json-patch+json"
    "Authorization" = "Bearer $pat"
}

# Make the REST API call to create the work item
$response = Invoke-RestMethod -Uri $url -Method Post -Body $bodyJson -Headers $headers

# Output the ID of the new work item
Write-Output "New work item ID: $($response.id)"
