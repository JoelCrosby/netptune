using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;

namespace DataPlane.Controllers
{
    public class BlobsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        private CloudBlobContainer GetCloudBlobContainer()
        {
            CloudStorageAccount storageAccount = new CloudStorageAccount(
               new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(
                "csba37cb19ff121x4c44x8a3",
                "TyRRK6alyjAeOJyyRnEnmtBpogZsev4BZbqCxKeTto73qbDaOw9z7Z6bgvjJdnlmmnaN8Lcfmnc8QHT5zP/ITw=="),
               true
            );

            // Create a blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Get a reference to a container named "mycontainer."
            CloudBlobContainer container = blobClient.GetContainerReference("mycontainer");

            return container;
        }
    }
}