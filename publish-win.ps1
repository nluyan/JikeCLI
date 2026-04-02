param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptRoot "src\JikeCLI\JikeCLI.csproj"
$publishPath = Join-Path $scriptRoot "src\JikeCLI\bin\$Configuration\net10.0\$Runtime\publish"

Write-Host "Publishing JikeCLI with Native AOT..."
Write-Host "Project: $projectPath"
Write-Host "Configuration: $Configuration"
Write-Host "Runtime: $Runtime"

dotnet publish $projectPath -c $Configuration -r $Runtime

if (-not (Test-Path $publishPath)) {
    throw "Publish output not found: $publishPath"
}

Write-Host ""
Write-Host "Publish completed."
Write-Host "Output: $publishPath"
