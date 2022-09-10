param($certName, $email, $buildEnv)

$groups = az group list --tag stack-name=shorturl | ConvertFrom-Json
if ($groups.Length -eq 0) {
    throw "Please create group with the following tags: stack-name=shorturl"
}
else {
    $resourceGroupName = $groups[0].name
}

$funcs = az functionapp list -g $groups.name | ConvertFrom-Json
$func = $funcs | Where-Object { $_.tags.'stack-environment' -eq $buildEnv }
$funcName = $func.name

Push-Location (Get-PAServer).Folder

$folderName = Get-Content .\current-account.txt
Push-Location $folderName

if (!(Test-Path $certName)) {
    New-PACertificate $certName -AcceptTOS -Contact $email
}

Push-Location $certName
$order = Get-Content order.json | ConvertFrom-Json
$EncodedString = $order.PfxPassB64U + "="
$DecodedString = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($EncodedString))

Write-Host "Uploading certificate..."
$cert = az functionapp config ssl upload --certificate-file cert.pfx `
    --certificate-password $DecodedString `
    --name $funcName `
    --resource-group $resourceGroupName | ConvertFrom-Json

Write-Host "Binding certificate..."
az functionapp config ssl bind --certificate-thumbprint $cert.Thumbprint --name $funcName --resource-group $resourceGroupName --ssl-type SNI
Pop-Location
Pop-Location
Pop-Location

# Use nslookup -q=TXT _acme-challenge.MYSUBDOMAINNAME.MYDOMAINNAME.com