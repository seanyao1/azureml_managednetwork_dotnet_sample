using Azure.Core;
using Azure.ResourceManager.Storage.Models;

namespace ManagedNetworkDemo
{
    public static class Constants
    {
        // You can locate your subscription ID on the Subscriptions blade of the Azure Portal (https://portal.azure.com).
        public const string SubscriptionId = "0caf7ec9-615a-4491-bad8-64ce023324e1"; //AML - Workspace R&D

        // Specify a resource group name of your choice. Specifying a new value will create a new resource group.
        public const string ResourceGroupName = "yyaoManagedNetworkDemo";
        public const string KeyVaultName = "yyao-nms-kv";

        // Storage account name. Using random value to avoid conflicts. Replace this with a storage account of your choice.
        public static readonly string StorageAccountName = $"yyaonmsdemostorage";

        // These values are used by the sample as defaults to create a new storage account. You can specify any location and any storage account type.
        public static readonly AzureLocation Location = AzureLocation.WestUS;
        public static readonly StorageSku StorageSku = new StorageSku(StorageSkuName.StandardGrs);
        public static readonly StorageKind Storagekind = StorageKind.StorageV2;

        // Specify the ARM id for Application Insights
        public const string AppInsightsArmId =
            "/subscriptions/0caf7ec9-615a-4491-bad8-64ce023324e1/resourcegroups/yyaoManagedNetworkDemo/providers/microsoft.insights/components/yyaodemo";

        public const string WorkspaceName = "yyao-nms-demo-ws";
    }
}