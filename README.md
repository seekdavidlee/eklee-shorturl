# ShortUrl
ShortUrl is a HTTP redirection service that runs as an Azure Function HTTP Trigger. You can host your this service with your own domain name. This allows you to share your host URL with a friendly id with your friends and family from which they can be redirected to. The id you pick should be something short and simple. For exampe, you can may have a URL on your domain such as https://mydomain.com/hotel and this may redirect to https://www.somehotel.com/bookings/reservation/1234566. You or your friends and family just have to know your domain name and you can provide just a simple id and off they go.

To create a short url, you will first need to submit a HTTP POST with the following payload to your domain with a Id. Be sure to include a API_KEY header you have defined in your configuration.

POST url: https://mydomain.com/hotel

```json
{
    "Url": "https://www.somehotel.com/bookings/reservation/1234566"
}
```

Once this is done, you can then just do a GET with the same url: 

GET url: https://mydomain.com/hotel

There will be a redirect from your URL to the URL as specified in your payload.

## Get started

1. First, you should have an Azure Subscription and fork this repo into your GitHub account.
2. Setup the following GitHub Environments dev and prod. If you think you are not going to do any development, simply create prod. Code is pulled from only the main branch for prod to build the Azure resources required (or any other branch name for dev).
3. In each of the environment (or just prod if you have prod), create the following:
    1. Secrets
        1. APIKEY: This can be anything like a GUID which you will use when you do sensitive operations like create/update or delete a URL.
        2. URLSTORAGECONNECTION: You should create an Azure Storage Account and use the Generate SAS feature to get a connection string to the Azure Storage Table. Be sure to name the table Url.
        3. AZURE_CREDENTIALS: This is the Azure credentials expressed in JSON format that contains the client id, client secret, subscription and AAD tenant id. Be sure to create a resource group with a tag of stack-name=shorturl as the CI/CD script will use this to lookup which resourcde group to create the Azure resources under. You should also ensure your service principal is assigned Contributor access to this resource group.
    2. Variables
        1. ALLOWEDIPLIST: This should be your home or office IP address. This prevents unauthorized access to sensitive operations to create/update or delete URL outside of your home or office.
        2.  PREFIX: This is the name for your created Azure resource.
4. Once you are ready, run the GitHub Action which will first build the Azure resources and then deploy the code.
5. Once code is deployed, you should now create a create an SSL cert. You will need to own a custom domain name. I recommend using https://letsencrypt.org/ which is free and a PowerShell module https://poshac.me. With both you can use the script inside the ssl folder: UploadCertAndSetDomainName.ps1. Pass in the domain name and your email and follow the instructions to configure a TXT record.
6. Once the TXT record is created, run ``` nslookup -q=TXT _acme-challenge.MYSUBDOMAINNAME.MYDOMAINNAME.com ``` to check if your TXT record is propogated before continuing in the script. The SSL cert will be uploaded to your Azure Function. 
7. Finally, you can refer to the PowerShell scripts in test folder on how to create/update or delete your URL.

### Azure Credentials

Your Azure Credential should look like the following:

```json
{
    "clientId": "",
    "clientSecret": "", 
    "subscriptionId": "",
    "tenantId": "" 
}
```

## Other Features

1. You can ensure only you can access the URL by configuring AllowedIPList which is a comma-deliminated IP list.
2. If you don't want to do a redirect and just want to lookup a key, just add a querystring ```?action=lookup```.
3. Swagger doc is included. Navigate to /swagger to view the API documentation. As you may already tell, the word "swagger" cannot be used as a id.
4. You can add additional words to be banned as id. Create a configuration named BannedList and add all the words into a comma-deliminated list as the value.