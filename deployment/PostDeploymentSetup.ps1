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

$secretName = "shorturl-func-app-store-$ENVIRONMENT"

$skipUpdate = $false
$secretVal = az keyvault secret show --vault-name $kv.Name --name $secretName --query "value" | ConvertFrom-Json
if ($LastExitCode -eq 0) {
    if ($secretVal.Contains(";AccountName=$storageName;")) {
        $skipUpdate = $true
    }
}

if (!$skipUpdate) {
    $storConnectionStr = "DefaultEndpointsProtocol=https;AccountName=$storageName;AccountKey=$key;EndpointSuffix=core.windows.net"
    az keyvault secret set --vault-name $kv.Name --name $secretName --value $storConnectionStr
    if ($LastExitCode -ne 0) {
        throw "Unable to set '$secretName'."
    }
}
else {
    Write-Host "Skip updating keyvault because value already exist."
}

$o = GetResource -solutionId $solutionId -environmentName $ENVIRONMENT -resourceId "app-id"
$clientId = (az resource show --ids $o.ResourceId --query "properties" | ConvertFrom-Json).principalId

az role assignment create --assignee $clientId --role "Storage Blob Data Owner" --scope $func.ResourceId
if ($LastExitCode -ne 0) {        
    throw "Unable to assign 'Storage Blob Data Owner'."
}

$func = GetResource -solutionId $solutionId -environmentName $ENVIRONMENT -resourceId "app-svc"
$a = (az functionapp config appsettings list --name $func.Name --resource-group $func.GroupName | ConvertFrom-Json) | Where-Object { $_.name -eq "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING" }
if ($LastExitCode -ne 0) {        
    throw "Unable to list appsettings."
}

if (!$a.value.StartsWith("@Microsoft.KeyVault(")) {
    $AppStorageConn = "shorturl-func-app-store-$ENVIRONMENT"
    $SharedKeyVaultName = $kv.Name

    Set-Content -Path .\temp -Value "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING=@Microsoft.KeyVault(VaultName=$SharedKeyVaultName;SecretName=$AppStorageConn)"

    az functionapp config appsettings set --name $func.Name `
        --resource-group $func.GroupName `
        --settings "@temp"

    if ($LastExitCode -ne 0) {        
        throw "Unable to set appconfig."
    }
    Remove-Item .\temp -Force
}