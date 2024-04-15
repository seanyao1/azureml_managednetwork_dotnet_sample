using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.MachineLearning;
using Azure.ResourceManager.MachineLearning.Models;
using Azure.ResourceManager.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using System;
using System.Threading.Tasks;
using KeyVaultProperties = Azure.ResourceManager.KeyVault.Models.KeyVaultProperties;

namespace ManagedNetworkDemo
{
    internal class Program
    {
        static Guid TenantId = Guid.Empty;

        static async Task Main(string[] args)
        {
            try
            {
                ArmClient armClient = new ArmClient(new DefaultAzureCredential());
                armClient.GetTenants();

                // Create a resource identifier, then get the subscription resource
                ResourceIdentifier resourceIdentifier =
                    new ResourceIdentifier($"/subscriptions/{Constants.SubscriptionId}");
                SubscriptionResource subscription = armClient.GetSubscriptionResource(resourceIdentifier);
                var sub = await subscription.GetAsync();
                subscription = sub.Value;
                Program.TenantId = subscription.Data.TenantId.Value;

                // Create resource group
                var resourceGroup = await CreateOrUpdateResourceGroup(subscription, Constants.ResourceGroupName);

                // Create storage account
                var storageAccount = await CreateOrUpdateStorageAccount(resourceGroup, Constants.StorageAccountName, Constants.StorageSku, Constants.Storagekind);

                // Create KV
                var keyvault = await CreateOrUpdateKeyVault(resourceGroup, Constants.KeyVaultName);

                // Create AzureML workspace
                var workspace = await CreateOrUpdateWorkspace(resourceGroup, Constants.WorkspaceName, storageAccount, keyvault, Constants.AppInsightsArmId);

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

        private static async Task<MachineLearningWorkspaceResource> CreateOrUpdateWorkspace(ResourceGroupResource resourceGroup, string workspaceName, StorageAccountResource storageAccount, KeyVaultResource keyvault, string appInsightsArmId)
        {
            Console.WriteLine("Creating AzureML workspace...");
            var workspaceCollection = resourceGroup.GetMachineLearningWorkspaces();
            var parameters = new MachineLearningWorkspaceData(Constants.Location)
            {
                PublicNetworkAccess = MachineLearningPublicNetworkAccess.Enabled,
                ApplicationInsights = Constants.AppInsightsArmId,
                Description = "Demo workspace for Managed Network Isolation feature",
                FriendlyName = Constants.WorkspaceName,
                Identity = new ManagedServiceIdentity(ManagedServiceIdentityType.SystemAssigned),
                KeyVault = keyvault.Id,
                StorageAccount = storageAccount.Id
            };

            var operation = await workspaceCollection.CreateOrUpdateAsync(WaitUntil.Completed, workspaceName, parameters);
            var ws = operation.Value;
            Console.WriteLine($"Workspace created: Id={ws.Id}");
            return ws;
        }

        private static async Task<KeyVaultResource> CreateOrUpdateKeyVault(ResourceGroupResource resourceGroup, string keyVaultName)
        {
            Console.WriteLine("Creating KeyVault...");
            var keyVaults = resourceGroup.GetKeyVaults();
            var properties =
                new KeyVaultProperties(TenantId, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard));
            var parameters = new KeyVaultCreateOrUpdateContent(Constants.Location, properties);
            var operation = await keyVaults.CreateOrUpdateAsync(WaitUntil.Completed, keyVaultName, parameters);
            var kv = operation.Value;
            Console.WriteLine($"KeyVault created: Id={kv.Id}");
            return kv;
        }

        private static async Task<StorageAccountResource> CreateOrUpdateStorageAccount(ResourceGroupResource resourceGroup, string storageAccountName, StorageSku storageSku, StorageKind storagekind)
        {
            Console.WriteLine("Creating storage account...");
            var parameters =
                new StorageAccountCreateOrUpdateContent(Constants.StorageSku, Constants.Storagekind, Constants.Location)
                {
                    AllowBlobPublicAccess = false
                };

            var accountCollection = resourceGroup.GetStorageAccounts();
            var operation = await accountCollection.CreateOrUpdateAsync(WaitUntil.Completed, storageAccountName, parameters);
            var storageAccount = operation.Value;
            Console.WriteLine($"Storage account created: Id={storageAccount.Id}");
            return storageAccount;
        }

        private static async Task<ResourceGroupResource> CreateOrUpdateResourceGroup(SubscriptionResource subscription, string resourceGroupName)
        {
            var operation = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed,
                resourceGroupName,
                new ResourceGroupData(Constants.Location));
            var rg = operation.Value;
            Console.WriteLine($"ResourceGroup created: Id={rg.Id}");
            return rg;
        }
    }
}
