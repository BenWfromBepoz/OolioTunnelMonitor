<#
.SYNOPSIS
    Provisions the Cloudflare API token on a target machine.
.DESCRIPTION
    Encrypts the supplied token with Windows DPAPI (LocalMachine scope) and
    writes it to the path expected by CloudflaredMonitor.
    Must be run as Administrator.
.PARAMETER Token
    The Cloudflare API bearer token (without the "Bearer" prefix).
.EXAMPLE
    .\Provision-Token.ps1 -Token "fDh15F3jC23mCbZWkY1WWMHIxKA7_-UfXdTPcH5H"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Token
)

#Requires -RunAsAdministrator

Add-Type -AssemblyName "System.Security"

$dir  = Join-Path $env:ProgramData "Bepoz\CloudflaredMonitor"
$file = Join-Path $dir "token.dat"

# Create directory if it does not exist
if (-not (Test-Path $dir)) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
}

# Encrypt with DPAPI LocalMachine scope
$bytes = [System.Text.Encoding]::UTF8.GetBytes($Token)
$enc   = [System.Security.Cryptography.ProtectedData]::Protect(
             $bytes,
             $null,
             [System.Security.Cryptography.DataProtectionScope]::LocalMachine)

[System.IO.File]::WriteAllBytes($file, $enc)

Write-Host "Token provisioned successfully to: $file" -ForegroundColor Green
Write-Host "The token is encrypted with DPAPI LocalMachine scope." -ForegroundColor Cyan
Write-Host "It can only be decrypted on this machine." -ForegroundColor Cyan