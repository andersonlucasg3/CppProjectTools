using namespace System.Collections.Generic

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function AddArgument()
{
    param (
        [ref][string[]] $Arguments,
        [string] $ParameterName,
        [string] $Value
    )

    if ([string]::IsNullOrEmpty($Value))
    {
        return
    }
    
    $Arguments.Value.Add("-$ParameterName=$Value")
}

function AddSwitch()
{
    param(
        [ref][string[]] $Arguments,
        [string] $ParameterName,
        [bool] $Add
    )

    if (-not $Add) 
    {
        return
    }

    $Arguments.Value.Add("-$ParameterName")
}

function CompileProjectTools 
{
    Write-Host "Compiling bundled BuildTool"

    $ProjectToolsSolution = "$(Get-Location)/ProjectTools/ProjectTools.sln"
    $ProjectToolsOutput = "$(Get-Location)/Binaries/DotNet/ProjectTools"
    
    $Output = dotnet publish $ProjectToolsSolution -c Debug -o $ProjectToolsOutput

    if (!$?)
    {
        Write-Host "BuildTool compilation failed:`n$Output" -ForegroundColor Red
    }
    else
    {
        Write-Host "BuildTool compiled successfully." -ForegroundColor Green
    }
}