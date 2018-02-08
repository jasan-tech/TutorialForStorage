using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Http;

namespace TutorialForStorage.Controllers
{
    public class BlobModel
    {
        public string ID { get; set; }
        public string Content { get; set; }
    }

    public class BlobsController : ApiController
    {
        private readonly string CONN_STRING = "AzureStorageConnectionString";
        private readonly string CONTAINER_NAME = "myblobs";
        private readonly CloudBlobClient _client;
        private readonly CloudBlobContainer _container;

        public BlobsController()
        {
            var connString = ConfigurationManager.AppSettings[CONN_STRING];
            var account = CloudStorageAccount.Parse(connString);
            _client = account.CreateCloudBlobClient();
            _container = _client.GetContainerReference(CONTAINER_NAME);
            _container.CreateIfNotExists();
        }

        // List all blob contents
        public async Task<IEnumerable<BlobModel>> Get()
        {
            var blobContents = new List<BlobModel>();
            var blobs = _container.ListBlobs();

            foreach (var blob in blobs)
            {
                if (blob is CloudBlockBlob cbb)
                {
                    var blobContent = await cbb.DownloadTextAsync();
                    var blobName = cbb.Name;

                    var model = new BlobModel
                    {
                        ID = blobName,
                        Content = blobContent
                    };

                    blobContents.Add(model);
                }
            }

            return blobContents;
        }

        // Download the contents of a single blob
        public async Task<string> Get(string id)
        {
            var blob = _container.GetBlockBlobReference(id);
            return await blob.DownloadTextAsync();
        }

        // Upload content to a new blob
        public async Task Put(string id, [FromBody]string content)
        {
            var blob = _container.GetBlockBlobReference(id);
            await blob.UploadTextAsync(content);
        }

        // Delete a blob
        public async Task Delete(string id)
        {
            var blob = _container.GetBlockBlobReference(id);
            await blob.DeleteIfExistsAsync();
        }
    }
}
