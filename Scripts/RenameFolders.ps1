param
(
    $baseFolder
)
$folders = Get-ChildItem -Directory -Path $baseFolder
foreach ($f in $folders)
{
    $oldName = [IO.Path]::GetFileName($f)
    $name = $oldName.Replace('_', ' ')

    $lessonPrefix = 'Lesson '
    if ($name.StartsWith($lessonPrefix))
    {
        $name = $name.Substring($lessonPrefix.Length)
    }

    $name = $name.Replace('  ', ' ')

    if ($name -ne $oldName)
    {
        $base = $f.Parent.FullName
        $ff = Join-Path $base $name
        write-host $ff
        ren $f.FullName $ff
    }
}