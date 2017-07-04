$buildNumber = $env:BUILD_BUILDNUMBER
$sourcesDir = $env:BUILD_SOURCESDIRECTORY
$fullHash = $env:BUILD_SOURCEVERSION

$nuspecs = Get-ChildItem -Path $sourcesDir\*.nuspec

if($nuspecs.Count -eq 0) 
{  
	throw "Could not find any nuspec files"
}

if($nuspecs.Count -gt 1)  
{
	throw "More than a single nuspec file was found"
}

$nuspec = $nuspecs[0]

[xml] $xml = Get-Content -Path $nuspec.FullName
$version = $xml.package.metadata.version
$versionParts = $version.split(".")
$hash = $fullHash.Substring(0,7)

$longVersion = "$($versionParts[0]).$($versionParts[1]).$($buildNumber)+$($hash)"
$shortVersion = "$($versionParts[0]).$($versionParts[1]).$($buildNumber)"

Write-Output "##vso[build.updatebuildnumber]$($longVersion)`r`n"
Write-Output "##vso[task.setvariable variable=ShortVersion;]$($shortVersion)`r`n"