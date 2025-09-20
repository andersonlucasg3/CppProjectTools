#!/usr/local/bin/pwsh

param(
    [Parameter(Mandatory = $true)]
    [string] $Project,

    [Parameter(Mandatory = $true)]
    [ValidateSet(
        "Clang",
        "VisualStudio"
    )]
    [string] $Generator,

    [Parameter()]
    [string[]] $Modules,

    [Parameter(Mandatory = $true)]
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

    [switch] $DisableMT,
    [switch] $SkipCompileProjectTools
)

$CommonsScript = Get-ChildItem -Path "**/ProjectTools/**/Commons.ps1"

. $CommonsScript

if (-not $SkipCompileProjectTools)
{
    CompileProjectTools

    if (-not $?)
    {
        return
    }
}

$Arguments = [List[string]]::new()
$Arguments.Add("GenCodeProject")

AddArgument ([ref]$Arguments) "Project" $Project
AddArgument ([ref]$Arguments) "Generator" $Generator
AddArgument ([ref]$Arguments) "Modules" $Modules
AddArgument ([ref]$Arguments) "Platform" $Platform
AddArgument ([ref]$Arguments) "Configuration" $Configuration
AddSwitch ([ref]$Arguments) "DisableMT" $DisableMT

& "$(Get-Location)/Binaries/DotNet/ProjectTools/ProjectTools" $Arguments

Exit $LASTEXITCODE