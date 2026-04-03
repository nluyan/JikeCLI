[CmdletBinding()]
param(
    [string]$ExePath = 'D:\Workshop\JikeCLI\src\JikeCLI\bin\Release\net10.0\win-x64\publish\JikeCLI.exe',
    [string]$BaseUrl = 'https://test5011.jikefw.com/',
    [string]$TenantId = '08dcf6f3-6f09-419b-8fdd-5747b5e5def8',
    [string]$UserName = 'V1Jk7qVa2L5YCP9vtgfeSQ==',
    [string]$PassWord = 'arwj71YS+ZrtCIZTjdKBMQ==',
    [string]$UploadFilePath = 'C:\Users\admin\OneDrive\Pictures\133949916608152304.jpg',
    [string]$Phone = '13800138000',
    [string]$RunDirectory = '',
    [switch]$DisableLoginFallback,
    [switch]$SkipOrderWithoutAttachment,
    [switch]$SkipOrderWithAttachment
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$utf8 = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = $utf8
[Console]::OutputEncoding = $utf8
$OutputEncoding = $utf8

function Write-Step {
    param([string]$Message)
    Write-Host ''
    Write-Host ('[{0}] {1}' -f (Get-Date -Format 'HH:mm:ss'), $Message)
}

function Normalize-Output {
    param([object[]]$Output)

    if ($null -eq $Output) {
        return @()
    }

    return @($Output | ForEach-Object {
        if ($null -eq $_) {
            ''
        }
        else {
            $_.ToString()
        }
    })
}

function Invoke-JikeCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [string]$StdIn,
        [Parameter(Mandatory = $true)]
        [string]$LogPath
    )

    Write-Step ("Running: {0} {1}" -f $ExePath, ($Arguments -join ' '))

    $output = if ($PSBoundParameters.ContainsKey('StdIn')) {
        $StdIn | & $ExePath @Arguments 2>&1
    }
    else {
        & $ExePath @Arguments 2>&1
    }

    $exitCode = $LASTEXITCODE
    $lines = Normalize-Output -Output $output

    $logLines = @(
        ('Command: {0} {1}' -f $ExePath, ($Arguments -join ' '))
        ('ExitCode: {0}' -f $exitCode)
        'Output:'
    ) + $lines

    Set-Content -Path $LogPath -Value $logLines -Encoding utf8
    $lines | ForEach-Object { Write-Host $_ }

    if ($exitCode -ne 0) {
        throw "Command failed with exit code $exitCode. See log: $LogPath"
    }

    return $lines
}

function Get-FirstMatch {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Lines,
        [Parameter(Mandatory = $true)]
        [string]$Pattern
    )

    foreach ($line in $Lines) {
        $match = [regex]::Match($line, $Pattern)
        if ($match.Success) {
            return $match.Groups[1].Value.Trim()
        }
    }

    return $null
}

function Save-ConfigFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigFilePath,
        [Parameter(Mandatory = $true)]
        [string]$Token,
        [Parameter(Mandatory = $true)]
        [string]$TokenType,
        [string]$RefreshToken,
        [int]$ExpiresIn
    )

    $configDirectory = Split-Path -Parent $ConfigFilePath
    New-Item -ItemType Directory -Path $configDirectory -Force | Out-Null

    $configObject = [ordered]@{
        Username = $UserName
        Tenant = $TenantId
        Token = $Token
        TokenType = $TokenType
        RefreshToken = $RefreshToken
        ExpiresIn = $ExpiresIn
        BaseUrl = $BaseUrl.TrimEnd('/')
        UpdatedAt = (Get-Date).ToString('o')
    }

    $configObject | ConvertTo-Json | Set-Content -LiteralPath $ConfigFilePath -Encoding utf8
}

function Invoke-LoginFallback {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigFilePath,
        [Parameter(Mandatory = $true)]
        [string]$LogPath
    )

    Write-Step 'CLI login failed, using direct HTTP login to continue downstream tests.'

    $headers = @{
        Tenantid = $TenantId
        platform = '0'
    }

    $bodyObject = @{
        userName = $UserName
        passWord = $PassWord
    }

    try {
        $response = Invoke-RestMethod -Uri ($BaseUrl.TrimEnd('/') + '/api/Check/AccountToken') -Method Post -Headers $headers -Body ($bodyObject | ConvertTo-Json) -ContentType 'application/json'
    }
    catch {
        $responseMessage = if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $reader.ReadToEnd()
        }
        else {
            $_.Exception.ToString()
        }

        Set-Content -LiteralPath $LogPath -Value @(
            'Fallback login failed.'
            $responseMessage
        ) -Encoding utf8

        throw "Fallback login failed. See log: $LogPath"
    }

    $rawJson = $response | ConvertTo-Json -Depth 10
    Set-Content -LiteralPath $LogPath -Value $rawJson -Encoding utf8

    $token = [string]$response.access_Token
    if ([string]::IsNullOrWhiteSpace($token)) {
        throw "Fallback login returned no access_Token. See log: $LogPath"
    }

    $tokenType = [string]$response.token_Type
    if ([string]::IsNullOrWhiteSpace($tokenType)) {
        $tokenType = 'Bearer'
    }

    Save-ConfigFile `
        -ConfigFilePath $ConfigFilePath `
        -Token $token `
        -TokenType $tokenType `
        -RefreshToken ([string]$response.refresh_Token) `
        -ExpiresIn ([int]$response.expires_In)
}

function Enable-LegacyConfigFallback {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceConfigFilePath,
        [Parameter(Mandatory = $true)]
        [string]$BackupDirectory
    )

    $legacyConfigFile = Join-Path (Join-Path $env:USERPROFILE '.jike') 'config.json'
    $legacyConfigDirectory = Split-Path -Parent $legacyConfigFile
    $backupConfigFile = Join-Path $BackupDirectory 'userprofile-config.backup.json'

    $script:LegacyConfigRestoreAction = 'remove'
    $script:LegacyConfigFile = $legacyConfigFile

    if (Test-Path -LiteralPath $legacyConfigFile) {
        New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
        Copy-Item -LiteralPath $legacyConfigFile -Destination $backupConfigFile -Force
        $script:LegacyConfigRestoreAction = 'restore'
        $script:LegacyConfigBackupFile = $backupConfigFile
    }
    else {
        $script:LegacyConfigBackupFile = $null
    }

    New-Item -ItemType Directory -Path $legacyConfigDirectory -Force | Out-Null
    Copy-Item -LiteralPath $SourceConfigFilePath -Destination $legacyConfigFile -Force
}

function Restore-LegacyConfigFallback {
    if ([string]::IsNullOrWhiteSpace($script:LegacyConfigFile)) {
        return
    }

    if ($script:LegacyConfigRestoreAction -eq 'restore' -and -not [string]::IsNullOrWhiteSpace($script:LegacyConfigBackupFile)) {
        Copy-Item -LiteralPath $script:LegacyConfigBackupFile -Destination $script:LegacyConfigFile -Force
        return
    }

    if ($script:LegacyConfigRestoreAction -eq 'remove' -and (Test-Path -LiteralPath $script:LegacyConfigFile)) {
        Remove-Item -LiteralPath $script:LegacyConfigFile -Force
    }
}

$repoRoot = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    (Get-Location).Path
}
else {
    Split-Path -Parent $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($RunDirectory)) {
    $RunDirectory = Join-Path (Join-Path $repoRoot 'sandbox') ('jikecli-tests\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}

if (-not (Test-Path -LiteralPath $ExePath)) {
    throw "CLI executable not found: $ExePath"
}

if (-not (Test-Path -LiteralPath $UploadFilePath)) {
    throw "Upload file not found: $UploadFilePath"
}

$runRoot = [System.IO.Path]::GetFullPath($RunDirectory)
$configHome = Join-Path $runRoot 'config-home'
$logDirectory = Join-Path $runRoot 'logs'

New-Item -ItemType Directory -Path $configHome -Force | Out-Null
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null

$previousConfigHome = $env:JIKE_CONFIG_HOME
$env:JIKE_CONFIG_HOME = $configHome
$script:LegacyConfigFile = $null
$script:LegacyConfigBackupFile = $null
$script:LegacyConfigRestoreAction = $null

try {
    Write-Step "Run root: $runRoot"
    Write-Step "Config home: $configHome"
    Write-Step "Logs: $logDirectory"

    $configFile = Join-Path (Join-Path $configHome '.jike') 'config.json'
    $loginMode = 'CLI'
    $loginSucceeded = $false
    $uploadSucceeded = $false
    $basicOrderSucceeded = $false
    $attachedOrderSucceeded = $false
    $failureMessages = New-Object System.Collections.Generic.List[string]
    $basicOrderError = $null
    $attachedOrderError = $null
    $uploadError = $null

    try {
        $loginOutput = Invoke-JikeCommand `
            -Arguments @(
                'login',
                '--username', $UserName,
                '--tenant', $TenantId,
                '--password', $PassWord
            ) `
            -LogPath (Join-Path $logDirectory '01-login.log')
    }
    catch {
        if ($DisableLoginFallback) {
            throw
        }

        Invoke-LoginFallback `
            -ConfigFilePath $configFile `
            -LogPath (Join-Path $logDirectory '01b-login-http-fallback.json')

        Enable-LegacyConfigFallback `
            -SourceConfigFilePath $configFile `
            -BackupDirectory (Join-Path $runRoot 'backup')

        $loginMode = 'HTTP fallback'
        $loginSucceeded = $true
    }

    if (-not (Test-Path -LiteralPath $configFile)) {
        throw "Login completed but config file was not created: $configFile"
    }

    $config = Get-Content -LiteralPath $configFile -Raw | ConvertFrom-Json
    if ([string]::IsNullOrWhiteSpace($config.Token)) {
        throw "Config file does not contain a token: $configFile"
    }
    $loginSucceeded = $true

    $fileIds = @()
    try {
        $uploadOutput = Invoke-JikeCommand `
            -Arguments @(
                'file',
                'upload',
                '--path', $UploadFilePath
            ) `
            -LogPath (Join-Path $logDirectory '02-file-upload.log')

        foreach ($line in $uploadOutput) {
            $match = [regex]::Match($line, 'ID:\s*(.+)$')
            if ($match.Success) {
                $fileIds += $match.Groups[1].Value.Trim()
            }
        }

        if ($fileIds.Count -eq 0) {
            throw 'Upload succeeded but no file ID was found in the command output.'
        }

        $uploadSucceeded = $true
    }
    catch {
        $uploadError = $_.Exception.Message
        $failureMessages.Add("file upload: $uploadError") | Out-Null
    }

    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $basicOrderId = $null
    $attachedOrderId = $null

    if (-not $SkipOrderWithoutAttachment -and $uploadSucceeded) {
        $basicRequiredTime = (Get-Date).AddHours(2).ToString('yyyy-MM-dd HH:mm:ss')
        $basicContent = "CLI automated test without attachment [$timestamp]"
        try {
            $basicOrderOutput = Invoke-JikeCommand `
                -Arguments @(
                    'order',
                    'add',
                    '--phone', $Phone,
                    '--urgency', '0',
                    '--content', $basicContent,
                    '--requiredTime', $basicRequiredTime
                ) `
                -LogPath (Join-Path $logDirectory '03-order-add-no-attachment.log')

            $basicOrderId = Get-FirstMatch -Lines $basicOrderOutput -Pattern 'ID:\s*(.+)$'
            $basicOrderSucceeded = $true
        }
        catch {
            $basicOrderError = $_.Exception.Message
            $failureMessages.Add("order add without attachment: $basicOrderError") | Out-Null
        }
    }

    if (-not $SkipOrderWithoutAttachment -and -not $uploadSucceeded) {
        $basicOrderError = 'Skipped because file upload did not succeed.'
        $failureMessages.Add("order add without attachment: $basicOrderError") | Out-Null
    }

    if (-not $SkipOrderWithAttachment -and $uploadSucceeded) {
        $attachedRequiredTime = (Get-Date).AddHours(3).ToString('yyyy-MM-dd HH:mm:ss')
        $attachedContent = "CLI automated test with attachment [$timestamp]"
        try {
            $attachedOrderOutput = Invoke-JikeCommand `
                -Arguments @(
                    'order',
                    'add',
                    '--phone', $Phone,
                    '--urgency', '1',
                    '--content', $attachedContent,
                    '--requiredTime', $attachedRequiredTime,
                    '--files', ($fileIds -join ',')
                ) `
                -LogPath (Join-Path $logDirectory '04-order-add-with-attachment.log')

            $attachedOrderId = Get-FirstMatch -Lines $attachedOrderOutput -Pattern 'ID:\s*(.+)$'
            $attachedOrderSucceeded = $true
        }
        catch {
            $attachedOrderError = $_.Exception.Message
            $failureMessages.Add("order add with attachment: $attachedOrderError") | Out-Null
        }
    }

    if (-not $SkipOrderWithAttachment -and -not $uploadSucceeded) {
        $attachedOrderError = 'Skipped because file upload did not succeed.'
        $failureMessages.Add("order add with attachment: $attachedOrderError") | Out-Null
    }

    Write-Step 'Test summary'

    [pscustomobject]@{
        ExePath = $ExePath
        BaseUrl = $BaseUrl
        TenantId = $TenantId
        UserName = $UserName
        LoginMode = $loginMode
        LoginSucceeded = $loginSucceeded
        LegacyConfigPathFallback = (-not [string]::IsNullOrWhiteSpace($script:LegacyConfigFile))
        UploadFilePath = $UploadFilePath
        UploadSucceeded = $uploadSucceeded
        UploadError = $uploadError
        Phone = $Phone
        OrderWithoutAttachmentSucceeded = $basicOrderSucceeded
        OrderWithoutAttachmentError = $basicOrderError
        OrderWithAttachmentSucceeded = $attachedOrderSucceeded
        OrderWithAttachmentError = $attachedOrderError
        ConfigFile = $configFile
        LogDirectory = $logDirectory
        FileIds = ($fileIds -join ',')
        BasicOrderId = $basicOrderId
        AttachedOrderId = $attachedOrderId
    } | Format-List

    if ($failureMessages.Count -gt 0) {
        throw ('Test completed with failures: ' + ($failureMessages -join '; '))
    }
}
finally {
    Restore-LegacyConfigFallback
    $env:JIKE_CONFIG_HOME = $previousConfigHome
}
