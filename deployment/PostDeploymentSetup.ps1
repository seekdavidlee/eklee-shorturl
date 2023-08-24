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

$solutionId = "shorturl"
$o = GetResource -solutionId $solutionId -environmentName $ENVIRONMENT -resourceId "app-id"

$clientId = az identity show --ids $o.ResourceId --query "clientId" | ConvertFrom-Json

$db = GetResource -solutionId $solutionId -environmentName $ENVIRONMENT -resourceId "app-database"
$func = GetResource -solutionId $solutionId -environmentName $ENVIRONMENT -resourceId "app-func-store"

az role assignment create --assignee $clientId --role "Storage Blob Data Owner" --scope $func.ResourceId
if ($LastExitCode -ne 0) {        
    throw "Unable to assign 'Storage Blob Data Owner'."
}

az role assignment create --assignee $clientId --role "Storage Table Data Contributor" --scope $db.ResourceId
if ($LastExitCode -ne 0) {        
    throw "Unable to assign 'Storage Table Data Contributor'."
}