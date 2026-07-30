function Get-ReleaseNotes {
    param (
        [Parameter(Mandatory=$true)]
        [string]$MarkdownFile
    )

    # Read markdown file content explicitly as UTF-8 to avoid platform-dependent defaults.
    # Fall back to UTF-16 only if the UTF-8 read returns no lines.
    $lines = [System.IO.File]::ReadAllLines($MarkdownFile, [System.Text.Encoding]::UTF8)
    if (-not $lines -or $lines.Count -eq 0) {
        $lines = [System.IO.File]::ReadAllLines($MarkdownFile, [System.Text.Encoding]::Unicode)
    }

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

    function Get-ReleaseHeaderCandidate {
        param(
            [Parameter()]
            [AllowEmptyString()]
            [string]$Line
        )

        $candidate = Get-LeadingNoiseFreeText -Line $Line
        $headerIndex = $candidate.IndexOf('####')
        if ($headerIndex -ge 0) {
            return $candidate.Substring($headerIndex)
        }

        return $null
    }

    function Get-ReleaseHeaderData {
        param(
            [Parameter()]
            [AllowEmptyString()]
            [string]$Line
        )

        $candidate = Get-ReleaseHeaderCandidate -Line $Line
        if (-not $candidate) {
            return $null
        }

        $normalizedHeaderLine = $candidate -replace "^####\s*", ""
        $normalizedHeaderLine = $normalizedHeaderLine -replace "\s*####\s*$", ""

        $headerParts = $normalizedHeaderLine -split " ", 2
        $versionText = $headerParts[0]

        $versionMatch = [regex]::Match($versionText, $versionPattern)
        if (-not $versionMatch.Success) {
            return $null
        }

        return [PSCustomObject]@{
            HeaderParts = $headerParts
            VersionText = $versionText
            VersionCore = $versionMatch.Groups['core'].Value
            VersionSuffix = $versionMatch.Groups['suffix'].Value
        }
    }
    
    # Find the first valid release header line.
    $headerData = $null
    $headerLineIndex = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $candidate = Get-ReleaseHeaderData -Line $lines[$i]
        if ($candidate) {
            $headerData = $candidate
            $headerLineIndex = $i
            break
        }
    }

    if ($null -eq $headerData) {
        throw "Unable to parse release notes from $MarkdownFile."
    }

    # Extract header text, then version/date.
    $headerParts = $headerData.HeaderParts
    $versionText = $headerData.VersionText

    $outputObject.Version = $versionText
    $outputObject.VersionCore = $headerData.VersionCore
    $outputObject.VersionSuffix = if ($headerData.VersionSuffix) { $headerData.VersionSuffix } else { '' }

    if ($headerParts.Count -ge 2) {
        $outputObject.Date = $headerParts[1]
    }

    # Grab release notes from this first section only.
    $releaseNotesEndLine = $lines.Count
    for ($i = $headerLineIndex + 1; $i -lt $lines.Count; $i++) {
        if (Get-ReleaseHeaderData -Line $lines[$i]) {
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
