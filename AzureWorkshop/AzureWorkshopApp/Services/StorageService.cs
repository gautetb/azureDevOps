using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AzureWorkshopApp.Helpers;
using AzureWorkshopApp.Models;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureWorkshopApp.Services
{
    public class StorageService : IStorageService
    {
        private readonly AzureStorageConfig _storageConfig;

        public object BlobContinuationToken { get; private set; }

        public StorageService(IOptions<AzureStorageConfig> storageConfig)
        {
            _storageConfig = storageConfig != null ? storageConfig.Value : throw new ArgumentNullException(nameof(storageConfig));
        }

        public AzureStorageConfigValidationResult ValidateConfiguration()
        {
            return StorageConfigValidator.Validate(_storageConfig);
        }

        public async Task<bool> UploadFileToStorage(Stream fileStream, string fileName)
        {
            var storageCred = new StorageCredentials(_storageConfig.AccountName, _storageConfig.AccountKey);
            var blobstorage = new CloudStorageAccount(storageCred, true);
            var cloudBlob = blobstorage.CreateCloudBlobClient();
            var container = cloudBlob.GetContainerReference(_storageConfig.ImageContainer);
            var blockBlob = container.GetBlockBlobReference(fileName);
            await blockBlob.UploadFromStreamAsync(fileStream);

            return await Task.FromResult(true);
        }

        public async Task<List<string>> GetImageUrls()
        {
            List<string> imageUrls = new List<string>();

            var storageCred = new StorageCredentials(_storageConfig.AccountName, _storageConfig.AccountKey);
            var blobstorage = new CloudStorageAccount(storageCred, true);
            var cloudBlob = blobstorage.CreateCloudBlobClient();
            var container = cloudBlob.GetContainerReference(_storageConfig.ImageContainer);

            BlobContinuationToken continuationToken = null;

            //Call ListBlobsSegmentedAsync and enumerate the result segment returned, while the continuation token is non-null.
            //When the continuation token is null, the last page has been returned and execution can exit the loop.
            do
            {
                //This overload allows control of the page size. You can return all remaining results by passing null for the maxResults parameter,
                //or by calling a different overload.
                BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync("", true, BlobListingDetails.All, 10, continuationToken, null, null);

                foreach (var blobItem in resultSegment.Results)
                {
                    imageUrls.Add(blobItem.StorageUri.PrimaryUri.ToString());
                }

                //Get the continuation token.
                continuationToken = resultSegment.ContinuationToken;
            }

            while (continuationToken != null);

            return await Task.FromResult(imageUrls);
        }
    }
}