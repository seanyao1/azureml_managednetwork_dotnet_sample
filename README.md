## DotNet example for creating an AzureML Workspace with managed network isolation
This is a simple DotNet example for calling Azure Resource Management APIs directly to create a workspace with dependencies and managed network isolation. The example also shows how to proactively provision the managed network in advance before you create any training compute or inferencing endpoints. This will reduce the chance of timeout for the first compute creation when you have a large set of outbound rules to set up for the managed network.  

This example doesn't use azure-dotnet-sdk, which is a wrapper of REST api, but the sdk is behind many API versions. Using HttpClient in .Net to call a rest API is pretty straight-forward. 

Note that, only for demo purpose, this example overly simplifies the Authentication part by using ```clientId``` and ```clientSecret``` directly from a configuration file. Please protect your credentials in your Prod environment using Azure KeyVault or similar products. 