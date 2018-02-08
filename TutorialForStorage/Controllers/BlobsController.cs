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
        private readonly string SERVER_PATH = "~/Images/";
        private readonly CloudBlobClient _client;
        private readonly CloudBlobContainer _container;

        // Initialize this controller with storage account and blob container
        public BlobsController()
        {
            var connString = CloudConfigurationManager.GetSetting(CONN_STRING);
            var account = CloudStorageAccount.Parse(connString);

            _client = account.CreateCloudBlobClient();
            _container = _client.GetContainerReference(CONTAINER_NAME);

            if (_container.CreateIfNotExists(BlobContainerPublicAccessType.Container))
            {
                Trace.WriteLine("Creating container {0}.", CONTAINER_NAME);
            }
            else
            {
                Trace.WriteLine("Using container {0}.", CONTAINER_NAME);
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
                Trace.WriteLine("No blobs found in blob container.  Uploading sample files.");
                await InitializeContainerWithSampleData();

                // Refresh enumeration after initializing
                blobs = _container.ListBlobs();
                blobsList.AddRange(blobs);
            }

            Trace.WriteLine("{0} blobs found in container.", blobsList.Count.ToString());

            foreach (var item in blobs)
            {
                if (item is CloudBlockBlob blob)
                {
                    var blobInfoString = $"Block blob with name '{blob.Name}', " +
                        $"content type '{blob.Properties.ContentType}', " +
                        $"size '{blob.Properties.Length}', " +
                        $"and URI '{blob.Uri}'";

                    blobsInfoList.Add(blobInfoString);
                    Trace.WriteLine(blobInfoString);
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
            return blobInfoString;
        }

        // Upload a file from server to Blob container
        public async Task<bool> UploadFile(string path)
        {
            using (var fileStream = File.OpenRead(path))
            {
                var filename = Path.GetFileName(path); // Trim fully pathed filename to just the filename
                if (File.Exists(path))
                {
                    var blockBlob = _container.GetBlockBlobReference(filename);
                    Trace.WriteLine("Uploading {0}.", filename);

                    await blockBlob.UploadFromStreamAsync(fileStream);

                    return await Task.FromResult(true);
                }
                else
                {
                    Trace.TraceError("File {0} not found.", path);
                    throw new FileNotFoundException();
                }
            }
        }

        // Download a blob to ~/Downloads/ on server
        public async Task<bool> DownloadFile(string blobName)
        {
            var blockBlob = _container.GetBlockBlobReference(blobName);

            using (var fileStream = File.OpenWrite(Path.Combine("downloads", blockBlob.Name)))
            {
                Trace.WriteLine("Downloading file {0}.", blockBlob.Name);
                await blockBlob.DownloadToStreamAsync(fileStream);

                Trace.WriteLine("Download complete.");
                return await Task.FromResult(true);
            }
        }

        public async Task Delete(string blobName)
        {
            var blob = _container.GetBlobReference(blobName);
            await blob.DeleteIfExistsAsync();
        }

        // Initialize blob container with all files in subfolder ~/Images/
        public async Task InitializeContainerWithSampleData()
        {
            var folderPath = HostingEnvironment.MapPath(SERVER_PATH);
            var folder = Directory.GetFiles(folderPath);

            foreach (var file in folder)
            {
                await UploadFile(file);
            }
        }
    }
}
