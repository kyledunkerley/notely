param([ValidateSet('Auto','ARM64','x64')][string]$Architecture = 'Auto')
$ErrorActionPreference = 'Stop'
if ($Architecture -eq 'Auto') { $Architecture = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq 'Arm64') { 'ARM64' } else { 'x64' } }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$target = Join-Path $PSScriptRoot "artifacts\desktop-$Architecture-$(Get-Date -Format yyyyMMdd-HHmmss)"
dotnet build "$PSScriptRoot\src\Acrux.csproj" -p:Platform=$Architecture -p:RuntimeIdentifier="win-$($Architecture.ToLowerInvariant())" -p:SelfContained=false -p:OutDir="$target\" --nologo
if ($LASTEXITCODE -ne 0) { throw 'Acrux failed to compile. See the build errors above.' }
Start-Process -FilePath (Join-Path $target 'Acrux.exe') -WorkingDirectory $target -Wait

