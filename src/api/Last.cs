using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Azure.Storage.Blobs;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using CMonitorAdministration;
using Azure.Storage.Blobs.Models;

namespace CamMonitorAPI
{
    [ApiController]
    [Route("last")]
    public class Last : ControllerBase
    {
        private BlobConStr _bcs;
        public Last(BlobConStr bcs)
        {
            _bcs = bcs;
        }

        [HttpGet]
        public async Task Get()
        {
            BlobServiceClient bsc = new BlobServiceClient(_bcs.AzureBlobConnectionString);
            BlobContainerClient bcc = bsc.GetBlobContainerClient("camera1");

            //Search for today
            string prefix = DateTime.UtcNow.Year.ToString("0000") + DateTime.UtcNow.Month.ToString("00") + DateTime.UtcNow.Day.ToString("00");
            Azure.Pageable<BlobItem> items = bcc.GetBlobs(prefix: prefix);

            //If there are none
            if (items.Count() == 0)
            {
                Response.StatusCode = 404;
                await Response.WriteAsync("There were no images saved for the current UTC day.");
                return;
            }

            //Find the newest (most recent)
            DateTime newest = new DateTime(1900, 1, 1);
            BlobItem newestBLOB = items.First();
            foreach (BlobItem bi in items)
            {
                string name = bi.Name.ToLower().Replace(".jpg", "");
                DateTime dt = TimeStamper.TimeStampToDateTime(name);
                if (dt > newest)
                {
                    newest = dt;
                    newestBLOB = bi;
                }
            }

            //Download it
            BlobClient bc = bcc.GetBlobClient(newestBLOB.Name);
            MemoryStream ms = new MemoryStream();
            await bc.DownloadToAsync(ms);

            //Write into response
            Response.StatusCode = 200;
            Response.Headers["Content-Type"] = "image/jpeg";
            ms.Position = 0;
            await ms.CopyToAsync(Response.Body);
            ms.Close();
        }

    }
}