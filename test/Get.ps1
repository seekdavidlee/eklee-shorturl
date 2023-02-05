param($Key, [switch]$Lookup)

$uri = "http://localhost:7068/$Key"
if ($Lookup) {
    $uri += "?action=lookup"
}
Write-Host "Uri: $uri"
Invoke-WebRequest -Uri $uri -MaximumRedirection 0