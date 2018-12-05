using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Netptune.Storage 
{

    public class StorageController 
    {

        public static string storageConnectionString = Environment.GetEnvironmentVariable ("storageconnectionstring");

        private CloudStorageAccount storageAccount = null;

        public int start () 
        {
            
            if (CloudStorageAccount.TryParse (storageConnectionString, out storageAccount))
            {
                
            }

            return 0;
        }

    }
}