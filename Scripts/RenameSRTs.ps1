param
(
    $baseFolder
)
$d = $baseFolder
$srt = Get-ChildItem -Path $d -Include '*.srt' -Recurse
foreach ($si in $srt)
{
    $s = $si.FullName
    $d = join-path $si.DirectoryName $si.Name.Replace('_', ' ').Replace('  ', ' ').Replace('[', '').Replace(']', '')
    $i = $d.IndexOf(' - lang en vs')
    if ($i -le 0)
    {
        $i = $d.IndexOf(' - lang en')
    }
    if ($i -ge 0)
    {
        $d = $d.Substring(0, $i).TrimEnd() + '.srt'
    }

    $d = $d.Replace(' .srt', '.srt')
    if ($s -ine $d)
    {
        write-host $d
        ren $s $d
    }
}