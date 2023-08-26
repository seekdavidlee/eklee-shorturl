param($ResourceGroupName, $AppName, $SharedKeyVaultName, $AppStorageConn)

$ErrorActionPreference = "Stop"

az functionapp config appsettings set --name $AppName --resource-group $ResourceGroupName --settings `
    "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING=@Microsoft.KeyVault(VaultName=$SharedKeyVaultName;SecretName=$AppStorageConn)" `
    "WEBSITE_CONTENTSHARE=share"
if ($LastExitCode -ne 0) {
    throw "Error with setting 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'."
}

dotnet publish Eklee.ShortUrl\Eklee.ShortUrl.csproj -c Release -o out

$zipFileName = "app.zip"
Compress-Archive out\* -DestinationPath $zipFileName -Force

az functionapp deployment source config-zip -g $ResourceGroupName -n $AppName --src $zipFileName

$apps = az functionapp config hostname list --resource-group $ResourceGroupName --webapp-name $AppName | ConvertFrom-Json
$domainName = $apps[0].name
$baseUrl = "https://$domainName"
"base_url=$baseUrl" >> $env:GITHUB_ENV