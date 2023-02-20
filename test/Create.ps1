param($Key, $Url, $AllowedIPList)
$dto = @{ Url = $Url; AllowedIPList = $AllowedIPList; }
$body = ConvertTo-Json $dto -Depth 10
Write-Host "Body: $body"
$Url="http://localhost:7068/$Key"
Write-Host "Url: $Url"
Invoke-WebRequest -Uri $Url -Body $body -Method Post -Headers @{ "API_KEY" = "testingonly"; }