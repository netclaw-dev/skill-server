function UpdateVersionAndReleaseNotes {
    param (
        [Parameter(Mandatory=$true)]
        [PSCustomObject]$ReleaseNotesResult,

        [Parameter(Mandatory=$true)]
        [string]$XmlFilePath
    )

    # Load XML
    $xmlContent = New-Object XML
    $xmlContent.Load($XmlFilePath)

    if (-not $ReleaseNotesResult.VersionCore) {
        throw "Get-ReleaseNotes did not return VersionCore for metadata update"
    }

    # Update VersionPrefix/VersionSuffix and PackageReleaseNotes
    $versionPrefixElement = $xmlContent.SelectSingleNode("//VersionPrefix")
    if (-not $versionPrefixElement) {
        throw "Directory.Build.props is missing VersionPrefix"
    }

    $versionPrefixElement.InnerText = $ReleaseNotesResult.VersionCore

    $versionSuffixElement = $xmlContent.SelectSingleNode("//VersionSuffix")
    if (-not $versionSuffixElement) {
        throw "Directory.Build.props is missing VersionSuffix"
    }

    $versionSuffixElement.InnerText = $ReleaseNotesResult.VersionSuffix

    if ($ReleaseNotesResult.VersionSuffix.Length -eq 0) {
        Write-Output "Updated release version suffix to empty (stable release)"
    }

    $packageReleaseNotesElement = $xmlContent.SelectSingleNode("//PackageReleaseNotes")
    $packageReleaseNotesElement.InnerText = $ReleaseNotesResult.ReleaseNotes

    # Save the updated XML
    $xmlContent.Save($XmlFilePath)
}

# Usage example:
# $notes = Get-ReleaseNotes -MarkdownFile "$PSScriptRoot\RELEASE_NOTES.md"
# $propsPath = Join-Path -Path (Get-Item $PSScriptRoot).Parent.FullName -ChildPath "Directory.Build.props"
# UpdateVersionAndReleaseNotes -ReleaseNotesResult $notes -XmlFilePath $propsPath
