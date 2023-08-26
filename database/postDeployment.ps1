param(
    [Parameter(Mandatory = $true)][string]$SUBSCRIPTION,
    [Parameter(Mandatory = $true)][string]$TENANT,
    [Parameter(Mandatory = $true)][string]$ENVIRONMENT,
    [Parameter(Mandatory = $false)][string]$REGION)

$ErrorActionPreference = "Stop"

function GetResource {
    param (
        [string]$solutionId,
        [string]$environmentName,
        [string]$resourceId
    )
    
    $obj = asm lookup resource --asm-rid $resourceId --asm-sol $solutionId --asm-env $environmentName  | ConvertFrom-Json
    if ($LastExitCode -ne 0) {
        Pop-Location
        throw "Unable to lookup resource."
    }
    
    return $obj
}

$appConfig = GetResource -solutionId "shared-services" -environmentName "prod" -resourceId "shared-app-configuration"

$ip = (Invoke-RestMethod https://api64.ipify.org?format=json).ip
az appconfig kv set --name $appConfig.Name --key "shorturlallowedip" --value $ip --label prod --auth-mode login --yes
if ($LastExitCode -ne 0) {
    Pop-Location
    throw "Unable to set Allowed IP."
}

$apiDev = [guid]::NewGuid().ToString("N") 
az appconfig kv set --name $appConfig.Name --key "shorturlapikey" --value $apiDev --label dev --auth-mode login --yes
if ($LastExitCode -ne 0) {
    Pop-Location
    throw "Unable to set shorturlapikey (dev)"
}

$apiProd = [guid]::NewGuid().ToString("N") 
az appconfig kv set --name $appConfig.Name --key "shorturlapikey" --value $apiProd --label prod --auth-mode login --yes
if ($LastExitCode -ne 0) {
    Pop-Location
    throw "Unable to set shorturlapikey (prod)"
}

$solutionId = "shorturl"
$db = GetResource -solutionId $solutionId -environmentName $ENVIRONMENT -resourceId "app-database"
$o = GetResource -solutionId $solutionId -environmentName $ENVIRONMENT -resourceId "app-id"
$clientId = az identity show --ids $o.ResourceId --query "clientId" | ConvertFrom-Json

az role assignment create --assignee $clientId --role "Storage Table Data Contributor" --scope $db.ResourceId
if ($LastExitCode -ne 0) {        
    throw "Unable to assign 'Storage Table Data Contributor'."
}

$kv = GetResource -solutionId "shared-services" -environmentName "prod" -resourceId "shared-key-vault"
az role assignment create --assignee $clientId --role "Key Vault Secrets User" --scope $kv.ResourceId
if ($LastExitCode -ne 0) {
    Pop-Location
    throw "Unable to assign 'Key Vault Secrets User' role."
}