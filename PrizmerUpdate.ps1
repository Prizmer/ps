
Write-Host "Welcome to PRIZMER installer!"

$subversionModuleVersion = Get-Module -ListAvailable -Name "Subversion" | Format-List -Property Version | Out-String
$subversionModuleVersion = $subversionModuleVersion.Trim().ToLower()
if ($subversionModuleVersion) {
    Write-Host "Subversion module exists, $subversionModuleVersion"
} 
else 
{
    Write-Host "No subversion module, installing... "
    Install-Module -Name "Subversion" -Force -AllowClobber
    Write-Host "Ready!"
    Write-Host ""
}

$repositoryName = Read-Host -Prompt 'Input GitHub repository name, like "tu_set4tm"'
$projectName = Read-Host -Prompt 'Input project name, like "tu_set"'

$remoteURL = "https://github.com/Prizmer/$repositoryName/trunk/$projectName/bin/Debug/";
$targetDirectory = "C:\Prizmer\$repositoryName"

# download target dirrectory recursively
svn export $remoteURL $targetDirectory


Invoke-Item $targetDirectory