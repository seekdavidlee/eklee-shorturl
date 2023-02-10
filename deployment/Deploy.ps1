param($ResourceGroupName, $AppName)

$ErrorActionPreference = "Stop"

dotnet publish Eklee.ShortUrl\Eklee.ShortUrl.csproj -c Release -o out

$zipFileName = "app.zip"
Compress-Archive out\* -DestinationPath $zipFileName -Force

az functionapp deployment source config-zip -g $ResourceGroupName -n $AppName --src $zipFileName