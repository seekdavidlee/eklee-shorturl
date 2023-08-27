param([Parameter(mandatory = $true)]$certName, [Parameter(mandatory = $true)]$email, [Parameter(mandatory = $true)]$buildEnv)

$func = asm lookup resource --asm-rid "app-svc" --asm-sol shorturl --asm-env $buildEnv --logging Info  | ConvertFrom-Json
$funcName = $func.Name
$resourceGroupName = $func.GroupName

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