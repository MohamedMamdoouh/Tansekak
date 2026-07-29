# Creates the Tansekak R2 bucket and CORS policy via Cloudflare API.
# Requires a Cloudflare API token with R2 bucket write permissions.
#
# Usage:
#   $env:CLOUDFLARE_API_TOKEN = "your-token"
#   .\scripts\setup-cloudflare-r2.ps1
#
# Optional overrides:
#   $env:R2__AccountId = "..."
#   $env:R2__BucketName = "tansekak-imports"
#   $env:R2_CORS_ORIGINS = "https://tansekak-production.up.railway.app,http://localhost:4200"

param(
    [string]$AccountId = $env:R2__AccountId,
    [string]$BucketName = $(if ($env:R2__BucketName) { $env:R2__BucketName } else { "tansekak-imports" }),
    [string]$ApiToken = $env:CLOUDFLARE_API_TOKEN,
    [string[]]$AllowedOrigins = @(
        "https://tansekak-production.up.railway.app",
        "http://localhost:4200"
    )
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ApiToken)) {
    throw "Set CLOUDFLARE_API_TOKEN to a Cloudflare API token with R2 write access."
}

if ([string]::IsNullOrWhiteSpace($AccountId)) {
    throw "Set R2__AccountId to your Cloudflare account ID."
}

if ($env:R2_CORS_ORIGINS) {
    $AllowedOrigins = $env:R2_CORS_ORIGINS.Split(",") | ForEach-Object { $_.Trim() } | Where-Object { $_ }
}

$headers = @{
    Authorization = "Bearer $ApiToken"
    "Content-Type" = "application/json"
}

function Invoke-CloudflareApi {
    param(
        [string]$Method,
        [string]$Path,
        [object]$Body
    )

    $uri = "https://api.cloudflare.com/client/v4$Path"
    $params = @{
        Method = $Method
        Headers = $headers
    }

    if ($null -ne $Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 10)
    }

    $response = Invoke-RestMethod @params -Uri $uri
    if (-not $response.success) {
        throw "Cloudflare API error on $Method $Path`: $($response.errors | ConvertTo-Json -Compress)"
    }

    return $response
}

Write-Host "Creating bucket '$BucketName' in account $AccountId..."
try {
    Invoke-CloudflareApi -Method POST -Path "/accounts/$AccountId/r2/buckets" -Body @{ name = $BucketName } | Out-Null
    Write-Host "Bucket created."
}
catch {
    if ($_.Exception.Message -match "already exists|409") {
        Write-Host "Bucket already exists; continuing."
    }
    else {
        throw
    }
}

$corsBody = @{
    rules = @(
        @{
            id = "TansekakBrowserUpload"
            allowed = @{
                origins = $AllowedOrigins
                methods = @("PUT")
                headers = @("Content-Type")
            }
            exposeHeaders = @("ETag")
            maxAgeSeconds = 3600
        }
    )
}

Write-Host "Applying CORS policy..."
Invoke-CloudflareApi -Method PUT -Path "/accounts/$AccountId/r2/buckets/$BucketName/cors" -Body $corsBody | Out-Null
Write-Host "CORS policy applied for origins: $($AllowedOrigins -join ', ')"

Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Cloudflare Dashboard -> R2 -> Manage R2 API Tokens -> Create token (Object Read & Write on $BucketName)"
Write-Host "2. Set R2__AccessKeyId and R2__SecretAccessKey on Railway / in .env"
Write-Host "3. Redeploy the app"
