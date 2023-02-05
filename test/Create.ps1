param($Key, $Url, $AllowedIPList)
$dto = @{ Url = $Url; AllowedIPList = $AllowedIPList; }
$body = ConvertTo-Json $dto -Depth 10
Write-Host "Body: $body"
Invoke-WebRequest -Uri "http://localhost:7068/$Key" -Body $body -Method Post -Headers @{ "API_KEY" = "testingonly"; }