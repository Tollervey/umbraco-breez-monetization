#requires -Version 5.0

<#
.SYNOPSIS
    Extracts the contents of a .NET project into a single human-readable text file.
    
.DESCRIPTION
    This script processes a .NET project starting from the directory containing the .csproj file.
    It generates:
    - A high-level folder structure tree (excluding .git, .vs, bin, obj, TestResults, and empty folders).
    - Root namespace information.
    - A simple ASCII architecture diagram (based on common .NET project patterns).
    - The contents of all relevant source files (.cs and .csproj), indented for readability.
    
    Excludes binary directories like bin, obj, .vs, packages, and non-text files.
    
    Output is written to a file named "{ProjectName}_CodeExtract.txt" in the current directory.
    
.NOTES
    Run this script from the directory containing the .csproj file.
    Assumes a single .csproj file in the current directory. If multiple exist, it uses the first one found.
    
.EXAMPLE
    .\ExtractProjectCode.ps1
#>

[CmdletBinding()]
param ()

# List of directories to exclude
$excludedDirs = @('bin', 'obj', '.vs', 'packages', 'node_modules', '.git', 'TestResults')

# Relevant file extensions
$relevantExtensions = @('.cs', '.csproj')

# Function to check if a directory is non-empty (has relevant files or non-empty subdirs, ignoring excluded)
function Is-DirectoryNonEmpty {
    param (
        [string]$Path
    )

    $items = Get-ChildItem -Path $Path -Force | Where-Object { $_.Name -notin $excludedDirs }

    # Has relevant files?
    if ($items | Where-Object { -not $_.PSIsContainer -and $_.Extension -in $relevantExtensions }) {
        return $true
    }

    # Has non-empty subdirs?
    foreach ($dir in $items | Where-Object { $_.PSIsContainer }) {
        if (Is-DirectoryNonEmpty -Path $dir.FullName) {
            return $true
        }
    }

    return $false
}

# Function to build a directory tree as a string (ASCII art)
function Get-DirectoryTree {
    param (
        [string]$Path,
        [string]$Indent = "",
        [bool]$Last = $true,
        [System.Collections.ArrayList]$TreeLines,
        [bool]$IsRoot = $true
    )

    if ($null -eq $TreeLines) {
        $TreeLines = New-Object System.Collections.ArrayList
    }

    $items = Get-ChildItem -Path $Path -Force | Where-Object { $_.Name -notin $excludedDirs }
    $fileItems = $items | Where-Object { -not $_.PSIsContainer -and $_.Extension -in $relevantExtensions }
    $dirItems = $items | Where-Object { $_.PSIsContainer } | Where-Object { Is-DirectoryNonEmpty -Path $_.FullName }

    # For non-root, check if empty; if empty and not root, skip adding
    if (-not $IsRoot -and $dirItems.Count -eq 0 -and $fileItems.Count -eq 0) {
        return
    }

    # Add current directory
    $line = "$Indent"
    if ($Last) {
        $line += "\-- "
    } else {
        $line += "|-- "
    }
    $line += (Split-Path $Path -Leaf)
    [void]$TreeLines.Add($line)

    $newIndent = "$Indent"
    if (-not $Last) {
        $newIndent += "|   "
    } else {
        $newIndent += "    "
    }

    # Process subdirectories
    for ($i = 0; $i -lt $dirItems.Count; $i++) {
        $isLastDir = ($i -eq $dirItems.Count - 1) -and ($fileItems.Count -eq 0)
        Get-DirectoryTree -Path $dirItems[$i].FullName -Indent $newIndent -Last $isLastDir -TreeLines $TreeLines -IsRoot $false
    }

    # Process files
    for ($i = 0; $i -lt $fileItems.Count; $i++) {
        $isLast = $i -eq $fileItems.Count - 1
        $fileLine = "$newIndent"
        if ($isLast) {
            $fileLine += "\-- "
        } else {
            $fileLine += "|-- "
        }
        $fileLine += $fileItems[$i].Name
        [void]$TreeLines.Add($fileLine)
    }

    if ($IsRoot) {
        return $TreeLines -join "`n"
    }
}

# Function to get root namespace from .csproj
function Get-RootNamespace {
    param (
        [string]$CsprojPath
    )

    try {
        [xml]$csproj = Get-Content -Path $CsprojPath
        $rootNamespace = $csproj.Project.PropertyGroup.RootNamespace
        if (-not $rootNamespace) {
            $rootNamespace = $csproj.Project.PropertyGroup.AssemblyName
        }
        if (-not $rootNamespace) {
            $rootNamespace = (Split-Path $CsprojPath -Leaf).Replace('.csproj', '')
        }
        return $rootNamespace
    } catch {
        Write-Warning "Unable to parse root namespace from .csproj. Using project file name as fallback."
        return (Split-Path $CsprojPath -Leaf).Replace('.csproj', '')
    }
}

# Function to generate a simple ASCII architecture diagram
# This is a generic representation; customize based on detected patterns if possible
function Get-ArchitectureDiagram {
    param (
        [string]$ProjectName
    )

    # Simple detection: Check for common folders to infer architecture
    $hasControllers = Test-Path (Join-Path $PWD 'Controllers')
    $hasModels = Test-Path (Join-Path $PWD 'Models')
    $hasViews = Test-Path (Join-Path $PWD 'Views')
    $hasServices = Test-Path (Join-Path $PWD 'Services')
    $hasData = Test-Path (Join-Path $PWD 'Data')

    $diagram = @"
Project: $ProjectName

+-------------------+
|   Presentation    |  (e.g., Controllers, Views, UI)
+-------------------+
          |
          v
+-------------------+
|   Business Logic  |  (e.g., Services, Managers)
+-------------------+
          |
          v
+-------------------+
|   Data Access     |  (e.g., Repositories, DbContext)
+-------------------+
          |
          v
+-------------------+
|   Data Storage    |  (e.g., Database, Files)
+-------------------+
"@

    if ($hasControllers -and $hasModels -and $hasViews) {
        $diagram += "`n`nDetected MVC-like structure."
    } elseif ($hasServices -and $hasData) {
        $diagram += "`n`nDetected layered architecture (Services + Data)."
    } else {
        $diagram += "`n`nNo specific architecture detected; using generic layered diagram."
    }

    return $diagram
}

# Main script logic
try {
    # Get current directory
    $currentDir = Get-Location

    # Find .csproj file
    $csprojFiles = Get-ChildItem -Path $currentDir -Filter '*.csproj' -File
    if ($csprojFiles.Count -eq 0) {
        throw "No .csproj file found in the current directory."
    } elseif ($csprojFiles.Count -gt 1) {
        Write-Warning "Multiple .csproj files found. Using the first one: $($csprojFiles[0].Name)"
    }
    $csprojPath = $csprojFiles[0].FullName
    $projectName = (Split-Path $csprojPath -Leaf).Replace('.csproj', '')

    # Output file path
    $outputFile = Join-Path $currentDir "$projectName_CodeExtract.txt"

    # Initialize output content
    $outputContent = New-Object System.Text.StringBuilder

    # Section 1: High-level folder structure
    [void]$outputContent.AppendLine("## High-Level Folder Structure")
    [void]$outputContent.AppendLine('```')
    $tree = Get-DirectoryTree -Path $currentDir
    [void]$outputContent.AppendLine($tree)
    [void]$outputContent.AppendLine('```')
    [void]$outputContent.AppendLine("")

    # Section 2: Root Namespace
    $rootNamespace = Get-RootNamespace -CsprojPath $csprojPath
    [void]$outputContent.AppendLine("## Root Namespace")
    [void]$outputContent.AppendLine($rootNamespace)
    [void]$outputContent.AppendLine("")

    # Section 3: Architecture Diagram
    [void]$outputContent.AppendLine("## Architecture Diagram (ASCII Representation)")
    [void]$outputContent.AppendLine('```')
    $archDiagram = Get-ArchitectureDiagram -ProjectName $projectName
    [void]$outputContent.AppendLine($archDiagram)
    [void]$outputContent.AppendLine('```')
    [void]$outputContent.AppendLine("")

    # Section 4: File Contents
    [void]$outputContent.AppendLine("## File Contents")
    [void]$outputContent.AppendLine("Below are the contents of all relevant source files, organized by relative path.")
    [void]$outputContent.AppendLine("")

    # Get all relevant files (text-based, exclude binaries)
    $allFiles = Get-ChildItem -Path $currentDir -Recurse -File | 
        Where-Object { 
            $_.Extension -in $relevantExtensions -and 
            $_.FullName -notmatch '\\(bin|obj|\\.vs|packages|node_modules|\\.git|TestResults)\\' 
        }

    foreach ($file in $allFiles) {
        $relativePath = $file.FullName.Substring($currentDir.Path.Length).TrimStart('\')
        [void]$outputContent.AppendLine("### File: $relativePath")
        [void]$outputContent.AppendLine('```' + $file.Extension.TrimStart('.'))
        $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
        # Indent content for readability (2 spaces per line)
        $indentedContent = $content -split "`n" | ForEach-Object { "  $_" }
        [void]$outputContent.AppendLine(($indentedContent -join "`n").TrimEnd())
        [void]$outputContent.AppendLine('```')
        [void]$outputContent.AppendLine("")
    }

    # Write to output file
    $outputContent.ToString() | Out-File -FilePath $outputFile -Encoding utf8
    Write-Host "Extraction complete. Output written to: $outputFile"

} catch {
    Write-Error "An error occurred: $_"
    exit 1
}