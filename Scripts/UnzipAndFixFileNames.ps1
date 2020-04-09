param(
    [string] $zipFolder,
    [string] $outputFolder
)

$zips = Get-ChildItem '*.zip' -Path $zipFolder
Add-Type -AssemblyName System.IO.Compression.FileSystem

foreach ($zip in $zips)
{
    $fileOutFolder = Join-Path $outputFolder $zip.Name.Replace('_', '').Replace(' Videos.zip', '')
    $a = [System.IO.Compression.ZipFile]::OpenRead($zip.FullName)
    try
    {
        write-host $zip
        if (!(Test-Path $fileOutFolder))
        {
            $dir = md $fileOutFolder
        }

        # NOTE: Entries lies sometimes!  If any file name contains a '"', then Entries returns nothing!  Use PowerArchiver to manually rename the bad file.
        foreach ($e in $a.Entries)
        {
            # If the file name contains ':', then Entry.Name only returns the name after the ':'.
            $origName = $e.FullName
            $outName = $origName.Replace('_', ' ').Replace(':', '-').Replace('?', '').Replace('\', '-').Replace('/', '-')
            for ($i = 1; $i -le 9; $i++)
            {
                $outName = $outName.Replace(' - lang en vs' + $i, '')
            }

            $outName = $outName.Replace(' .mp4', '.mp4')
            if ($outName -ne $origName)
            {
                write-host "  Renamed $origName to $outName."
            }

            $fullOutName = Join-Path $fileOutFolder $outName
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($e, $fullOutName, $true)
        }
    }
    finally
    {
        $a.Dispose()
    }
}