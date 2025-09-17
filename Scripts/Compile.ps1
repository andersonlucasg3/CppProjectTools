#!/usr/local/bin/pwsh

using namespace System.Collections.Generic

param(
    [string] $Project,

    [Parameter()]
    [string[]] $Modules,

    [ValidateSet(
        "iOS",
        "tvOS",
        "visionOS",
        "macOS",
        "Windows",
        "Android"
    )]
    [string] $Platform,

    [ValidateSet(
        "Debug",
        "Release"
    )]
    [string] $Configuration = "Debug",

    [ValidateSet(
        "Any",
        "x64",
        "Arm64"
    )]
    [string] $Architecture = "x64",

    [switch] $Clean,
    [switch] $Recompile,
    [switch] $Relink,
    [switch] $PrintCompileCommands,
    [switch] $PrintLinkCommands,
    [switch] $ProjectToolsOnly
)

. ./ProjectTools/Scripts/Commons.ps1

CompileProjectTools

if (-not $?)
{
    return;
}

if ($ProjectToolsOnly)
{
    Write-Host "Project tools only compilation requested. Exiting."

    return;
}

$Arguments = [List[string]]::new()

if ($Clean)
{
    $Arguments.Add("Clean")
}
else
{
    $Arguments.Add("Compile")
}

AddArgument ([ref]$Arguments) "Project" $Project
AddArgument ([ref]$Arguments) "Modules" $Modules
AddArgument ([ref]$Arguments) "Platform" $Platform
AddArgument ([ref]$Arguments) "Configuration" $Configuration
AddSwitch ([ref]$Arguments) "Recompile" $Recompile
AddSwitch ([ref]$Arguments) "PrintCompileCommands" $PrintCompileCommands
AddSwitch ([ref]$Arguments) "PrintLinkCommands" $PrintLinkCommands

Write-Host "Location: $(Get-Location)"

& "$(Get-Location)/Binaries/DotNet/ProjectTools/ProjectTools" $Arguments

Exit $LASTEXITCODE