﻿namespace BuildTool.ProjectGeneration.VisualStudio.ProjectXml;

public class Parameter(string InName, string InValue)
{
    public override string ToString()
    {
        return $"""
        {InName}="{InValue}"
        """;
    }
}
