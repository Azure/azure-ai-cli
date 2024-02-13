param(
    [string]$token,
    [string]$owner,
    [string]$repo,
    [int]$issueNumber,
    [string]$newDescription
)

$headers = @{
    Authorization = "Bearer $token"
    Accept = "application/vnd.github.v3+json"
}

$body = @{
    body = $newDescription
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repo/issues/$issueNumber" -Method Patch -Body $body -ContentType "application/json" -Headers $headers