param($Key)
Invoke-WebRequest -Uri "http://localhost:7068/$Key" -Method Delete -Headers @{ "API_KEY" = "testingonly"; }