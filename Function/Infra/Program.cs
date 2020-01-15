using System.Collections.Generic;
using System.Threading.Tasks;
using Pulumi;
using Pulumi.Azure.AppService;
using Pulumi.Azure.AppService.Inputs;
using Pulumi.Azure.Core;
using Pulumi.Azure.Storage;

namespace Infra
{
    internal static class Program
    {
        static Task<int> Main()
        {
            return Deployment.RunAsync(() => {

                // Create an Azure Resource Group
                var resourceGroup = new ResourceGroup("doaw20-rg");

                // Create an Azure Storage Account
                var storageAccount = new Account("doaw20st", new AccountArgs
                {
                    ResourceGroupName = resourceGroup.Name,
                    AccountReplicationType = "LRS",
                    AccountTier = "Standard",
                    //EnableHttpsTrafficOnly = true
                });

                var appServicePlan = new Plan("doaw20-asp", new PlanArgs
                {
	                ResourceGroupName = resourceGroup.Name,
	                Kind = "FunctionApp",
	                Sku = new PlanSkuArgs
	                {
		                Tier = "Dynamic",
		                Size = "Y1"
	                }
                });

                var container = new Container("zips", new ContainerArgs
                {
	                StorageAccountName = storageAccount.Name,
	                ContainerAccessType = "private"
                });

                var blob = new ZipBlob("zip", new ZipBlobArgs
                {
	                StorageAccountName = storageAccount.Name,
	                StorageContainerName = container.Name,
	                Type = "block",
	                Content = new FileArchive("../HelloFunc/bin/Debug/netcoreapp3.1/publish")
                });

                var codeBlobUrl = SharedAccessSignature.SignedBlobReadUrl(blob, storageAccount);

                var app = new FunctionApp("doaw20-app", new FunctionAppArgs
                {
	                ResourceGroupName = resourceGroup.Name,
	                AppServicePlanId = appServicePlan.Id,
	                AppSettings =
	                {
		                { "runtime", "dotnet" },
		                { "WEBSITE_RUN_FROM_PACKAGE", codeBlobUrl }
	                },
	                StorageConnectionString = storageAccount.PrimaryConnectionString,
	                Version = "~3"
                });

                // Export the connection string for the storage account
                return new Dictionary<string, object?>
                {
                    { "connectionString", storageAccount.PrimaryConnectionString },
                    { "endpoint", Output.Format($"https://{app.DefaultHostname}/api/HelloPulumi?name=DevOps@Work20") }
                };
            });
        }
    }
}
