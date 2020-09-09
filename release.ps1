# Env setup ---------------
if ($PSScriptRoot -match '.+?\\bin\\?') {
    $dir = $PSScriptRoot + "\"
}
else {
    $dir = $PSScriptRoot + "\bin\"
}

$copy = $dir + "\copy\BepInEx\plugins" 
$plugins = $dir + "\BepInEx\plugins"

# Create releases ---------
function CreateZip ($pluginFile)
{
    Remove-Item -Force -Path ($dir + "\copy") -Recurse -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $copy

    Copy-Item -Path $pluginFile.FullName -Destination $copy -Recurse -Force 

    # the replace removes .0 from the end of version up until it hits a non-0 or there are only 2 version parts remaining (e.g. v1.0 v1.0.1)
    $ver = (Get-ChildItem -Path ($copy) -Filter "*.dll" -Recurse -Force)[0].VersionInfo.FileVersion.ToString() -replace "^([\d+\.]+?\d+)[\.0]*$", '${1}'

    Compress-Archive -Path ($copy + "\..\") -Force -CompressionLevel "Optimal" -DestinationPath ($dir + $pluginFile.BaseName + "_" + "v" + $ver + ".zip")
}

foreach ($pluginFile in Get-ChildItem -Path $plugins) 
{
    try
    {
        CreateZip ($pluginFile)
    }
    catch 
    {
        # retry
        CreateZip ($pluginFile)
    }
}

Remove-Item -Force -Path ($dir + "\copy") -Recurse
