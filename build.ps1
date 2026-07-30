. "$PSScriptRoot\scripts\getReleaseNotes.ps1"
. "$PSScriptRoot\scripts\bumpVersion.ps1"

######################################################################
# Step 1: Grab release notes and update solution metadata
######################################################################
$releaseNotes = Get-ReleaseNotes -MarkdownFile (Join-Path -Path $PSScriptRoot -ChildPath "RELEASE_NOTES.md")

Write-Output "Updating release metadata to $($releaseNotes.Version)"

if ($releaseNotes.VersionSuffix.Length -gt 0) {
    Write-Output "Release suffix: $($releaseNotes.VersionSuffix)"
} else {
    Write-Output "Release is stable: no prerelease suffix"
}

# inject release notes into Directory.Buil
UpdateVersionAndReleaseNotes -ReleaseNotesResult $releaseNotes -XmlFilePath (Join-Path -Path $PSScriptRoot -ChildPath "Directory.Build.props")

Write-Output "Updated Directory.Build.props from $($releaseNotes.Version)"
