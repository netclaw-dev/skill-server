function Get-ReleaseNotes {
    param (
        [Parameter(Mandatory=$true)]
        [string]$MarkdownFile
    )

    # Read markdown file content
    $content = Get-Content -Path $MarkdownFile -Raw

    # Split content based on headers
    $sections = $content -split "####"

    $versionPattern = '^(?<core>(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*))(?:-(?<suffix>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+(?<build>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$'

    # Output object to store result
    $outputObject = [PSCustomObject]@{
        Version      = $null
        VersionCore  = $null
        VersionSuffix = $null
        Date         = $null
        ReleaseNotes = $null
    }

    # Check if we have at least 3 sections (1. Before the header, 2. Header, 3. Release notes)
    if ($sections.Count -ge 3) {
        $header = $sections[1].Trim()
        $releaseNotes = $sections[2].Trim()

        # Extract version and date from the header
        $headerParts = $header -split " ", 2
        if ($headerParts.Count -ge 1) {
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
        }

        $outputObject.ReleaseNotes = $releaseNotes
    } else {
        throw "Unable to parse release notes from $MarkdownFile."
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
