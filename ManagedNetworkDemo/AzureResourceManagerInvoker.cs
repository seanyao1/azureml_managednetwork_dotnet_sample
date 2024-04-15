using Azure.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ManagedNetworkDemo
{
    public interface IAzureResourceManagerInvoker
    {
        Task<JObject> GetSubscriptionAsync(Guid subscriptionId);
        Task CreateResourceGroupAsync(Guid subscriptionId, string resourceGroupName, string locaction);
        Task<JObject> CreateStorageAccount(Guid subscriptionId, string resourceGroupName, string storageAccountName, string location);
        Task<JObject> CreateKeyVault(Guid subscriptionId, string resourceGroupName, string keyVaultName, string location, string tenantId);
        Task<JObject> CreateAzureMLWorkspace(
                       Guid subscriptionId,
                       string resourceGroupName,
                       string workspaceName,
                       string location,
                       string storageResourceId,
                       string kvResourceId,
                       string appInsightsResourceId);
       Task<JObject> CallApi(ResourceIdentifier resourceId, HttpMethod method, JObject body, string apiVersion);
    }

    public class AzureResourceManagerInvoker : IAzureResourceManagerInvoker
    {
        ITokenFectcher _tokenFetcher;
        HttpClient _httpClient;

        public AzureResourceManagerInvoker(
            ITokenFectcher tokenFetcher)
        {
            _tokenFetcher = tokenFetcher;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://management.azure.com/")
            };
        }

        public async Task<JObject> GetSubscriptionAsync(Guid subscriptionId)
        {
            var resourceId = ResourceIdentifier.Parse($"/subscriptions/{subscriptionId}");
            var response = await CallApi(resourceId, HttpMethod.Get, null, "2019-10-01");

            Console.WriteLine($"Get subscription. Response={response}");
            return response;
        }

        public async Task CreateResourceGroupAsync(Guid subscriptionId, string resourceGroupName, string locaction)
        {
            var resourceId = ResourceIdentifier.Parse($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}");
            Console.WriteLine($"Creating resource group if not exists. ResourceId={resourceId}");
            var body = new JObject
            {
                { "location", locaction }
            };

            var response = await CallApi(resourceId, HttpMethod.Put, body, "2019-10-01");

            Console.WriteLine($"Created resource group. Response={response}");
        }

        public async Task<JObject> CreateStorageAccount(Guid subscriptionId, string resourceGroupName, string storageAccountName, string location)
        {
            var resourceId = ResourceIdentifier.Parse($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}");
            Console.WriteLine($"Creating storage account. ResourceId={resourceId}");

            var body = new JObject
            {
                { "location", location },
                { "sku", new JObject {
                        { "name", "Standard_LRS" }
                    }
                },
                { "kind", "StorageV2" },
                { "properties", new JObject
                    {
                        { "publicNetworkAccess", "Disabled" },
                        { "allowBlobPublicAccess", false }
                    }
                }
            };

            var response = await CallApi(resourceId, HttpMethod.Put, body, "2023-05-01");
            Console.WriteLine($"Created stroage account. Response={response}");
            return response;
        }

        public async Task<JObject> CreateKeyVault(Guid subscriptionId, string resourceGroupName, string keyVaultName, string location, string tenantId)
        {
            var resourceId = ResourceIdentifier.Parse($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}");
            Console.WriteLine($"Creating key vault. ResourceId={resourceId}");

            var body = new JObject
            {
                { "location", location },
                { "properties", new JObject
                    {
                        { "tenantId",  tenantId },
                        { "sku", new JObject
                        {
                                { "family", "A" },
                                { "name", "standard" }
                            }
                        },
                        { "accessPolicies", new JArray
                            {
                                new JObject
                                {
                                    { "tenantId", tenantId },
                                    { "objectId", "00000000-0000-0000-0000-000000000000" },
                                    { "permissions", new JObject
                                    {
                                            { "keys", new JArray { "all" } },
                                            { "secrets", new JArray { "all" } },
                                            { "certificates", new JArray { "all" } }
                                        }
                                    }
                                }
                            }
                        },
                        { "enabledForDeployment", true },
                        { "enabledForDiskEncryption", true},
                        { "enabledForTemplateDeployment", true},
                        { "publicNetworkAccess", "Disabled" }
                    }
                }
            };

            var response = await CallApi(resourceId, HttpMethod.Put, body, "2023-07-01");
            Console.WriteLine($"Created key vault. Response={response}");
            return response;
        }

        public async Task<JObject> CreateAzureMLWorkspace(
            Guid subscriptionId,
            string resourceGroupName,
            string workspaceName,
            string location,
            string storageResourceId,
            string kvResourceId,
            string appInsightsResourceId)
        {
            var resourceId = ResourceIdentifier.Parse($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.MachineLearningServices/workspaces/{workspaceName}");
            Console.WriteLine($"Creating AzureML workspace. ResourceId={resourceId}");

            var rule1 = new JObject
            {
                { "type", "FQDN" },
                { "destination", "pypi.org" },
            };

            var rule2 = new JObject
            {
                { "type", "ServiceTag" },
                { "destination", new JObject
                    {
                        { "serviceTag", "AzureContainerRegistry" },
                        { "protocol", "tcp" },
                        { "portRanges", "443,5443-5444" }
                    }
                },
            };

            var body = new JObject
            {
                { "location", location },
                { "identity", new JObject
                    {
                        { "type", "SystemAssigned" }
                    }
                },
                { "properties", new JObject
                    {
                        { "friendlyName", workspaceName },
                        { "description", "My Azure ML demo workspace" },
                        { "storageAccount", storageResourceId },
                        { "keyVault", kvResourceId },
                        { "applicationInsights", appInsightsResourceId},
                        { "publicNetworkAccess", "Disabled" },
                        { "managedNetwork", new JObject
                            {
                                { "isolationMode", "AllowOnlyApprovedOutbound" },
                                { "outboundRules", new JObject
                                    {
                                        { "pypi", rule1 },
                                        { "acr-serviceTag", rule2 }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var response = await CallApi(resourceId, HttpMethod.Put, body, "2024-04-01");
            Console.WriteLine($"Created AzureML workspace. Response={response}");
            return response;
        }

        public async Task<JObject> CallApi(ResourceIdentifier resourceId, HttpMethod method, JObject body, string apiVersion)
        {
            var token = await _tokenFetcher.GetAccessTokenAsync();
            var uri = $"{resourceId}?api-version={apiVersion}";
            var request = new HttpRequestMessage(method, uri);
            request.Headers.Add("Authorization", "Bearer " + token);
            if (body != null)
            {
                request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
            }

            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                var jsonString = await response.Content.ReadAsStringAsync();
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var resource = JObject.Parse(jsonString);
                        return resource;
                    case HttpStatusCode.Created:
                        resource = JObject.Parse(jsonString);
                        return await PollUtillSuccessfullyProvisioned(resource).ConfigureAwait(false);
                    case HttpStatusCode.Accepted:
                        return await PollUtillOperationComplete(response).ConfigureAwait(false);
                    case HttpStatusCode.NoContent:
                        return null;
                    default:
                        throw new HttpRequestException($"Error calling API. Status code={response.StatusCode}, body={jsonString}");
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Error calling API: {e}");
                throw;
            }
        }
        private async Task<JObject> PollUtillSuccessfullyProvisioned(JObject response)
        {
            var resourceId = ResourceIdentifier.Parse(response["id"].Value<string>());
            Console.WriteLine($"Polling provisioningState, resourceId={resourceId}");

            var provisioningState = response["properties"]["provisioningState"].Value<string>();
            while (provisioningState != "Succeeded")
            {
                await Task.Delay(5000); // Demo code. Please use expo backoff in Prod code
                Console.Write("..");
                response = await CallApi(resourceId, HttpMethod.Get, null, "2019-06-01");
                provisioningState = response["properties"]["provisioningState"].Value<string>();
            }

            Console.WriteLine($"Provisioning completed. ProvisioningState={provisioningState}");

            return response;
        }
        private async Task<JObject> PollUtillOperationComplete(HttpResponseMessage response)
        {
            var operationUri = response.Headers.Location;
            Console.WriteLine($"Polling operation status. OperationUri={operationUri.AbsolutePath}");
            while (true)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, operationUri);
                var token = await _tokenFetcher.GetAccessTokenAsync();
                request.Headers.Add("Authorization", "Bearer " + token);
                var operationResponse = await _httpClient.SendAsync(request);
                var jsonString = await operationResponse.Content.ReadAsStringAsync();
                if (operationResponse.StatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine($"Operation completed.");
                    return JObject.Parse(jsonString);
                }
                else if (operationResponse.StatusCode == HttpStatusCode.Accepted)
                {
                    Console.Write("..");
                    await Task.Delay(5000); // Demo code. Please use expo backoff in Prod code
                }
                else
                {
                    throw new HttpRequestException($"Error calling API. Status code={operationResponse.StatusCode}");
                }
            }

        }
    }
}
