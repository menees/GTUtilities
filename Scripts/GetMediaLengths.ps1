param
(
    [string] $baseFolder,
    [Boolean] $scanChildFolders = $false,
    [string[]] $fileMasks = @('*.mp4'),
    [Boolean] $showFiles = $false
)

# Don't allow uninitialized variable or property usage.
Set-StrictMode -Version Latest

# Make the script stop if any command has an error.
$ErrorActionPreference = "Stop"

if (!(Test-Path $baseFolder))
{
    throw "Base folder '$baseFolder' was not found."
}

$folders = @($baseFolder)
if ($scanChildFolders)
{
    $folders = @(Get-ChildItem -Path $baseFolder -Directory | Select-Object -ExpandProperty FullName | Sort-Object)
}

# Based on code from https://social.technet.microsoft.com/Forums/Windows/en-US/bad2dbb1-5deb-48b8-8f8c-45e2b353dba0/how-do-i-get-video-file-duration-in-powershell-script?forum=winserverpowershell
$shellApp = New-Object -ComObject Shell.Application 
foreach ($folder in $folders)
{
    $folderName = [IO.Path]::GetFileName($folder)
    if ($showFiles)
    {
        Write-Host $folderName
    }

    $folderTotalTimeSpan = [TimeSpan]::Zero
    $folderTotalCount = 0
    $shellNamespace = $shellApp.Namespace($folder)
    foreach ($filter in $fileMasks)
    {
        $files = @(Get-ChildItem -Path $folder -Filter $filter -File | Sort-Object)
        foreach ($file in $files)
        {
            # All supported GetDetailsOf "columns" are listed at http://stackoverflow.com/a/37061433/1882616.
            $LengthColumn = 27
            $shellParsedName = $shellNamespace.ParseName($File)
            $Length = $shellNamespace.GetDetailsOf($shellParsedName, $LengthColumn)
            $fileTimeSpan = [TimeSpan]::Parse($Length)
            $folderTotalTimeSpan += $fileTimeSpan
            $folderTotalCount += 1

            if ($showFiles)
            {
                Write-Host "`t$file`t$length"
            }
        }
    }

    Write-Host "$folderName`t$folderTotalCount`t$folderTotalTimeSpan"
    if ($showFiles)
    {
        Write-Host
    }
}
