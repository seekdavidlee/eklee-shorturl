$ErrorActionPreference = "Stop"
    
# This is the rg where the application should be deployed
$groups = az group list | ConvertFrom-Json
$appResourceGroup = ($groups | Where-Object { $_.tags.'stack-name' -eq 'shorturl' }).name
"appResourceGroup=$appResourceGroup" >> $env:GITHUB_ENV
