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

    function Get-LeadingNoiseFreeText {
        param(
            [Parameter()]
            [AllowEmptyString()]
            [string]$Line
        )

        while (-not [string]::IsNullOrEmpty($Line)) {
            $firstCharacter = $Line[0]
            if ([char]::IsWhiteSpace($firstCharacter) -or [char]::IsControl($firstCharacter) -or [System.Char]::GetUnicodeCategory($firstCharacter) -eq [System.Globalization.UnicodeCategory]::Format) {
                $Line = $Line.Substring(1)
                continue
            }

            break
        }

        return $Line
    }
    
    # Find the first valid release header line.
    $headerLine = $null
    $headerLineIndex = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $candidate = Get-LeadingNoiseFreeText -Line $lines[$i]

        if ($candidate.StartsWith('####')) {
            $headerLine = $candidate
            $headerLineIndex = $i
            break
        }
    }

    if ($null -eq $headerLine) {
        throw "Unable to parse release notes from $MarkdownFile."
    }

    # Extract header text, then version/date.
    $headerLine = $headerLine -replace "^####\s*", ""
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
        $candidate = Get-LeadingNoiseFreeText -Line $lines[$i]

        if ($candidate.StartsWith('####')) {
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
