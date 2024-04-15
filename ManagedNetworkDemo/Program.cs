using Azure;
using Azure.Core;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ManagedNetworkDemo
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Security warning: The client secret should be stored securely and should not be hardcoded in code or a file.
                var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, reloadOnChange: true).Build();
                var tenantId = config["tenantId"];

                // Create a token fetcher to get the access token
                var tokenFectcher = new TokenFetcher();
                await tokenFectcher.InitializeAsync(tenantId, config["clientId"], config["clientKey"]).ConfigureAwait(false);  
                
                // Create a helper for calling ARM REST API:
                var armInvoker = new AzureResourceManagerInvoker(tokenFectcher);
                
                //Get the subscription resource
                var subscription = await armInvoker.GetSubscriptionAsync(Constants.SubscriptionId).ConfigureAwait(false);

                // Create resource group
                await armInvoker.CreateResourceGroupAsync(Constants.SubscriptionId, Constants.ResourceGroupName, Constants.Location.ToString()).ConfigureAwait(false);

                // Create KV
                var keyVault = await armInvoker.CreateKeyVault(Constants.SubscriptionId, Constants.ResourceGroupName, Constants.KeyVaultName, Constants.Location.ToString(), tenantId).ConfigureAwait(false);

                // Create storage account
                var storageAccount = await armInvoker.CreateStorageAccount(Constants.SubscriptionId, Constants.ResourceGroupName, Constants.StorageAccountName, Constants.Location.ToString()).ConfigureAwait(false);
                
                // Create AzureML workspace, using managed network (AOAO mode)
                var workspace = await armInvoker.CreateAzureMLWorkspace(
                    Constants.SubscriptionId, 
                    Constants.ResourceGroupName, 
                    Constants.WorkspaceName, 
                    Constants.Location.ToString(),
                    storageResourceId: storageAccount.Value<string>("id"),
                    kvResourceId: keyVault.Value<string>("id"),
                    appInsightsResourceId: Constants.AppInsightsArmId
                    ).ConfigureAwait(false);

                // Call AzureML to proactively provision the managed network now.
                var body = new JObject
                {
                    {"includeSpark", false }
                };
                var wsResourceId = workspace.Value<string>("id");
                var provisionAction = ResourceIdentifier.Parse($"{wsResourceId}/provisionManagedNetwork");
                Console.WriteLine($"Start provisioning managed network for workspace {wsResourceId}..");
                var result = await armInvoker.CallApi(provisionAction, HttpMethod.Post, body, "2024-04-01");
                Console.WriteLine($"Completed provisioning managed network for workspace {wsResourceId}, returned result: {result}");

                //now youc can start create compute, online endopints in the workspace using the managed vnet. 
                //....

                Console.WriteLine("All done!");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
            finally
            {
                Console.Out.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
