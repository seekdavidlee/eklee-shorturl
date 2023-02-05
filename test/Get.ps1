param($Key)
Invoke-WebRequest -Uri "http://localhost:7068/$Key" -MaximumRedirection 0