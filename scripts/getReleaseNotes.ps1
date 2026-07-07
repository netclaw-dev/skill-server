function Get-ReleaseNotes {
    param (
        [Parameter(Mandatory=$true)]
        [string]$MarkdownFile
    )

    # Read markdown file content
    $lines = Get-Content -Path $MarkdownFile

    $versionPattern = '^(?<core>(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*))(?:-(?<suffix>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+(?<build>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$'

    # Output object to store result
    $outputObject = [PSCustomObject]@{
        Version      = $null
        VersionCore  = $null
        VersionSuffix = $null
        Date         = $null
        ReleaseNotes = $null
    }

    if (-not $lines -or $lines.Count -eq 0) {
        throw "Unable to parse release notes from $MarkdownFile."
    }

    # Find the first non-empty line to find the latest release header.
    $headerLine = $null
    $headerLineIndex = 0
    while ($headerLineIndex -lt $lines.Count) {
        $candidate = $lines[$headerLineIndex].Trim()

        if (-not [string]::IsNullOrWhiteSpace($candidate)) {
            $headerLine = $candidate
            break
        }

        $headerLineIndex++
    }

    if ($null -eq $headerLine) {
        throw "Unable to parse release notes from $MarkdownFile."
    }

    # Strip BOMs or other non-content leading chars.
    $headerLine = $headerLine.TrimStart([char]0xFEFF, [char]0x200B)

    if (-not $headerLine.StartsWith("####")) {
        throw "Unable to parse release notes from $MarkdownFile."
    }

    # Extract header text, then version/date.
    $headerLine = $headerLine.Substring(4).Trim()
    $headerLine = $headerLine -replace "\s*####\s*$", ""

    $headerParts = $headerLine -split " ", 2
    $versionText = $headerParts[0]

    if ($versionText -notmatch $versionPattern) {
        throw "Invalid release version '$versionText' in $MarkdownFile."
    }

    $outputObject.Version = $versionText
    $outputObject.VersionCore = $matches.core
    $outputObject.VersionSuffix = if ($matches.suffix) { $matches.suffix } else { '' }

    if ($headerParts.Count -ge 2) {
        $outputObject.Date = $headerParts[1]
    }

    # Grab release notes from this first section only.
    $releaseNotesEndLine = $lines.Count
    for ($i = $headerLineIndex + 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Trim().StartsWith("####")) {
            $releaseNotesEndLine = $i
            break
        }
    }

    if ($releaseNotesEndLine -gt $headerLineIndex + 1) {
        $outputObject.ReleaseNotes = ($lines[($headerLineIndex + 1)..($releaseNotesEndLine - 1)] -join "`r`n").Trim()
    }
    else {
        $outputObject.ReleaseNotes = ""
    }

    # Return the output object
    return $outputObject
}

# Call function example:
#$result = Get-ReleaseNotes -MarkdownFile "$PSScriptRoot\RELEASE_NOTES.md"
#Write-Output "Version: $($result.Version)"
#Write-Output "Date: $($result.Date)"
#Write-Output "Release Notes:"
#Write-Output $result.ReleaseNotes
