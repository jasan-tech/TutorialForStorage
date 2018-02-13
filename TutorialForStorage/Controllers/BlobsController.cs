using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using System.Diagnostics;
using System.IO;
using System.Web.Hosting;

namespace TutorialForStorage.Controllers
{
    public class BlobsController : ApiController
    {
        private readonly string CONN_STRING = "AzureStorageConnectionString";
        private readonly string CONTAINER_NAME = "quickstart";
        private readonly string UPLOAD_PATH = "~/Images/";
        private readonly string DOWNLOAD_PATH = "~/Downloads";
        private readonly CloudBlobClient _client;
        private readonly CloudBlobContainer _container;

        // Initialize this controller with storage account and blob container
        public BlobsController()
        {
            var connString = CloudConfigurationManager.GetSetting(CONN_STRING);
            var account = CloudStorageAccount.Parse(connString);

            _client = account.CreateCloudBlobClient();
            _container = _client.GetContainerReference(CONTAINER_NAME);

            if (_container.CreateIfNotExists(BlobContainerPublicAccessType.Container)) //requires Azure Storage Emulator 5.2 or higher: https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator#get-the-storage-emulator 
            {
                Trace.WriteLine($"QuickStart: Creating Azure storage blob container '{CONTAINER_NAME}'.");
            }
            else
            {
                Trace.WriteLine($"QuickStart: Using container '{CONTAINER_NAME}'.");
            }
        }

        // List all blob contents
        // Local: http://localhost:58673/api/blobs
        // route /api/blobs
        public async Task<IEnumerable<string>> Get()
        {
            var blobsInfoList = new List<string>();
            var blobs = _container.ListBlobs(); // Use ListBlobsSegmentedAsync for containers with large numbers of files
            var blobsList = new List<IListBlobItem>(blobs);

            if (blobsList.Count == 0)
            {
                Trace.WriteLine($"QuickStart: No blobs found in blob container.  Uploading sample files from '{UPLOAD_PATH}'");
                await InitializeContainerWithSampleData();

                // Refresh enumeration after initializing
                blobs = _container.ListBlobs();
                blobsList.AddRange(blobs);
            }

            Trace.WriteLine($"QuickStart: {blobsList.Count.ToString()} blobs found in container.");

            foreach (var item in blobs)
            {
                if (item is CloudBlockBlob)
                {
                    var blob = (CloudBlockBlob)item;
                    var blobInfoString = $"Block blob with name '{blob.Name}', " +
                        $"content type '{blob.Properties.ContentType}', " +
                        $"size '{blob.Properties.Length}', " +
                        $"and URI '{blob.Uri}'";

                    blobsInfoList.Add(blobInfoString);
                    Trace.WriteLine($"QuickStart: {blobInfoString}");
                }
            }

            return blobsInfoList;
        }

        // Display properties from a single blob
        public string Get(string name)
        {
            // Retrieve reference to a blob by filename, e.g. "photo1.jpg".
            var blob = _container.GetBlockBlobReference(name);
            var blobInfoString = $"Block blob with name '{blob.Name}', " +
                        $"content type '{blob.Properties.ContentType}', " +
                        $"size '{blob.Properties.Length}', " +
                        $"and URI '{blob.Uri}'";
            Trace.WriteLine($"QuickStart: {blobInfoString}");
            return blobInfoString;
        }

        // Upload a file from server to Blob container
        [Route("api/blobs/upload")]
        public async Task<bool> UploadFile(string path)
        {
            var filePathOnServer = Path.Combine(HostingEnvironment.MapPath(UPLOAD_PATH), path);

            using (var fileStream = File.OpenRead(filePathOnServer))
            {
                var filename = Path.GetFileName(path); // Trim fully pathed filename to just the filename
                if (File.Exists(filePathOnServer))
                {
                    var blockBlob = _container.GetBlockBlobReference(filename);
                    Trace.WriteLine($"QuickStart: Uploading '{filename}' from '{UPLOAD_PATH}' to '{CONTAINER_NAME}'.");

                    await blockBlob.UploadFromStreamAsync(fileStream);
                    Trace.WriteLine($"QuickStart: Upload of file '{filename}' complete.");

                    return await Task.FromResult(true);
                }
                else
                {
                    Trace.TraceError($"QuickStart: File '{path}' not found.");
                    throw new FileNotFoundException();
                }
            }
        }

        // Download a blob to ~/Downloads/ on server
        [HttpGet]
        [Route("api/blobs/download")]
        public async Task<bool> DownloadFile(string blobName)
        {
            var blockBlob = _container.GetBlockBlobReference(blobName);

            var downloadsPathOnServer = Path.Combine(HostingEnvironment.MapPath(DOWNLOAD_PATH), blockBlob.Name);

            using (var fileStream = File.OpenWrite(downloadsPathOnServer))
            {
                Trace.WriteLine($"QuickStart: Downloading file '{blockBlob.Name}' to '{DOWNLOAD_PATH}'.");
                await blockBlob.DownloadToStreamAsync(fileStream);

                Trace.WriteLine($"QuickStart: Download of file '{blockBlob.Name}' complete.");
                return await Task.FromResult(true);
            }
        }

        // Delete a blob by name.
        public async Task Delete(string blobName)
        {
            var blockBlob = _container.GetBlobReference(blobName);
            await blockBlob.DeleteIfExistsAsync();
            Trace.WriteLine($"QuickStart: Delete of file '{blockBlob.Name}' complete.");
        }

        // Initialize blob container with all files in subfolder ~/Images/
        public async Task InitializeContainerWithSampleData()
        {
            var folderPath = HostingEnvironment.MapPath(UPLOAD_PATH);
            var folder = Directory.GetFiles(folderPath);

            foreach (var file in folder)
            {
                await UploadFile(file);
            }
        }
    }
}
