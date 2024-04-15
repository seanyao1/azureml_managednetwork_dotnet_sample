using Azure.Core;
using System;
using System.IO;

namespace ManagedNetworkDemo
{
    public static class Constants
    {
        // You can locate your subscription ID on the Subscriptions blade of the Azure Portal (https://portal.azure.com).
        public static Guid SubscriptionId = Guid.Parse("0caf7ec9-615a-4491-bad8-64ce023324e1"); //AML - Workspace R&D

        // Specify a resource group name of your choice. Specifying a new value will create a new resource group.
        public const string ResourceGroupName = "azuremlManagedNetworkDemo";
        public static readonly string KeyVaultName = $"demo-kv-{GetRandomSuffix()}";

        // Storage account name. Using random value to avoid conflicts. Replace this with a storage account of your choice.
        public static readonly string StorageAccountName = $"demostorage{GetRandomSuffix()}";

        // These values are used by the sample as defaults to create a new storage account. You can specify any location and any storage account type.
        public static readonly AzureLocation Location = AzureLocation.WestUS;

        // Specify the ARM id for Application Insights
        public const string AppInsightsArmId =
            "/subscriptions/0caf7ec9-615a-4491-bad8-64ce023324e1/resourcegroups/yyaoManagedNetworkDemo/providers/microsoft.insights/components/yyaodemo";

        public static string WorkspaceName = $"my-demo-ws-{GetRandomSuffix()}";

        private static string GetRandomSuffix()
        {
            var suffix = Path.GetFileNameWithoutExtension(Path.GetTempFileName());
            return suffix.Substring(0, Math.Min(10, suffix.Length)).ToLowerInvariant();
        }
    }
}