param(
    [string]$BUILD_ENV)

$ErrorActionPreference = "Stop"

dotnet tool install --global azsolutionmanager --version 0.1.5-beta

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

function GetResourceAndSetInOutput {
    param ($SolutionId, $ResourceId, $EnvName, $OutputKey, [switch]$UseId, [switch]$ThrowIfMissing)

    $json = asm lookup resource --asm-rid $ResourceId --asm-sol $SolutionId --asm-env $EnvName --logging Info
    if ($LastExitCode -ne 0) {
        throw "Error with resource $ResourceId lookup."
    }

    if (!$json) {

        if ($ThrowIfMissing) {
            throw "Value for $OutputKey is missing! Inputs: [$ResourceId, $SolutionId, $EnvName]"
        }
        return
    }

    $obj = $json | ConvertFrom-Json

    if ($UseId) {
        $objValue = $obj.ResourceId
    }
    else {
        $objValue = $obj.Name
    }

    if ($ThrowIfMissing -and !$objValue) {
        throw "Value for $OutputKey is missing!"
    }

    "$OutputKey=$objValue" >> $env:GITHUB_OUTPUT

    return
}
    
$solutionId = "shorturl"
$json = asm lookup group --asm-sol $solutionId --asm-env $BUILD_ENV --logging Info
if ($LastExitCode -ne 0) {
    throw "Error with group lookup."
}
$obj = $json | ConvertFrom-Json
$groupName = $obj.Name
"resourceGroupName=$groupName" >> $env:GITHUB_OUTPUT
"prefix=su" >> $env:GITHUB_OUTPUT

GetResourceAndSetInOutput -SolutionId $solutionId -EnvName $BUILD_ENV -ResourceId 'app-database' -OutputKey "appStorageName" -ThrowIfMissing
GetResourceAndSetInOutput -SolutionId $solutionId -EnvName $BUILD_ENV -ResourceId 'app-apm' -OutputKey "appInsightsName"
GetResourceAndSetInOutput -SolutionId $solutionId -EnvName $BUILD_ENV -ResourceId 'app-svcplan' -OutputKey "appPlanName"
GetResourceAndSetInOutput -SolutionId $solutionId -EnvName $BUILD_ENV -ResourceId 'app-svc' -OutputKey "appName"
GetResourceAndSetInOutput -SolutionId $solutionId -EnvName $BUILD_ENV -ResourceId 'app-id' -OutputKey "appId"

$appConfig = GetResource -solutionId "shared-services" -environmentName "prod" -resourceId "shared-app-configuration"

$ip = az appconfig kv show --name $appConfig.Name --key "shorturlallowedip" --label prod --auth-mode login --query value | ConvertFrom-Json
if ($LastExitCode -ne 0) {
    Pop-Location
    throw "Unable to get allowedip"
}
"allowedIPList=$ip" >> $env:GITHUB_OUTPUT

$apiKey = az appconfig kv show --name $appConfig.Name --key "shorturlapikey" --label $BUILD_ENV --auth-mode login --query value | ConvertFrom-Json
if ($LastExitCode -ne 0) {
    Pop-Location
    throw "Unable to get apikey"
}
"apiKey=$apiKey" >> $env:GITHUB_OUTPUT


