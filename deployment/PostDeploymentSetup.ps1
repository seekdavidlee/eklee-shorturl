param(
    [Parameter(Mandatory = $true)][string]$ENVIRONMENT)

$ErrorActionPreference = "Stop"

function GetResource {
    param (
        [string]$solutionId,
        [string]$environmentName,
        [string]$resourceId
    )
        
    $obj = asm lookup resource --asm-rid $resourceId --asm-sol $solutionId --asm-env $environmentName  | ConvertFrom-Json
    if ($LastExitCode -ne 0) {        
        throw "Unable to lookup resource."
    }
        
    return $obj
}

$solutionId = "shared-services"
$environmentName = "prod"
$kv = GetResource -solutionId $solutionId -environmentName $environmentName -resourceId "shared-key-vault"

$solutionId = "shorturl"
$func = GetResource -solutionId $solutionId -environmentName $ENVIRONMENT -resourceId "app-func-store"

# Configure secrets for storage connection string
$storageName = $func.Name
$key = az storage account keys list --account-name $storageName --query "[0].value" | ConvertFrom-Json
if ($LastExitCode -ne 0) {
    throw "Unable to list key from '$storageName'."
}

$storConnectionStr = "DefaultEndpointsProtocol=https;AccountName=$storageName;AccountKey=$key;EndpointSuffix=core.windows.net"
$secretName = "shorturl-func-app-store-$ENVIRONMENT"
az keyvault secret set --vault-name $kv.Name --name $secretName --value $storConnectionStr
if ($LastExitCode -ne 0) {
    throw "Unable to set '$secretName'."
}

$o = GetResource -solutionId $solutionId -environmentName $ENVIRONMENT -resourceId "app-svc"
$clientId = (az resource show --ids $o.ResourceId --query "identity" | ConvertFrom-Json).principalId

az role assignment create --assignee $clientId --role "Storage Blob Data Owner" --scope $func.ResourceId
if ($LastExitCode -ne 0) {        
    throw "Unable to assign 'Storage Blob Data Owner'."
}

az role assignment create --assignee $clientId --role "Key Vault Secrets User" --scope $kv.ResourceId
if ($LastExitCode -ne 0) {
    Pop-Location
    throw "Unable to assign 'Key Vault Secrets User' role."
}

$db = GetResource -solutionId $solutionId -environmentName $ENVIRONMENT -resourceId "app-database"
$o = GetResource -solutionId $solutionId -environmentName $ENVIRONMENT -resourceId "app-id"
$clientId = az identity show --ids $o.ResourceId --query "clientId" | ConvertFrom-Json

az role assignment create --assignee $clientId --role "Storage Table Data Contributor" --scope $db.ResourceId
if ($LastExitCode -ne 0) {        
    throw "Unable to assign 'Storage Table Data Contributor'."
}