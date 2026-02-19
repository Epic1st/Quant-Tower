param(
    [Parameter(Mandatory = $true)]
    [string]$RelaySecret,

    [string]$BaseUrl = 'http://127.0.0.1:8000'
)

$ErrorActionPreference = 'Stop'

$health = Invoke-WebRequest -UseBasicParsing "$BaseUrl/health"
Write-Output "HEALTH_STATUS=$($health.StatusCode)"
Write-Output $health.Content

$postBody = '{"symbol":"EURUSD","side":"buy","price":1.2345}'
$post = Invoke-RestMethod -Method Post -Uri "$BaseUrl/signal" -Headers @{ 'X-Auth' = $RelaySecret } -ContentType 'application/json' -Body $postBody
Write-Output "POST_OK=$($post.ok) ID=$($post.id)"

$encoded = [uri]::EscapeDataString($RelaySecret)
$poll = Invoke-RestMethod -Method Get -Uri "$BaseUrl/signal?id=0&auth=$encoded"
Write-Output "POLL_UPDATED=$($poll.updated) ID=$($poll.id)"
Write-Output ($poll | ConvertTo-Json -Compress)

try {
    Invoke-WebRequest -UseBasicParsing -Uri "$BaseUrl/signal?id=0&auth=wrong" | Out-Null
} catch {
    Write-Output "BAD_AUTH_STATUS=$($_.Exception.Response.StatusCode.value__)"
}
