$year = (Get-Date).Year
$uri = "http://localhost:7068/stats/$year"
Write-Host "Uri: $uri"
Invoke-WebRequest -Uri $uri -UseBasicParsing -ContentType "application/json"