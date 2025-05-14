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
            LastImageFinder lif = new LastImageFinder(_bcs.AzureBlobConnectionString);
            ImageInfo ii;
            try
            {
                ii = lif.FindLastImage("camera1", true, 7);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 404;
                Response.Headers["Content-Type"] = "plain/text";
                await Response.WriteAsync("There was a fatal error while trying to find the last image! Message: " + ex.Message);
                return;
            }

            if (ii.Image == null)
            {
                Response.StatusCode = 404;
                Response.Headers["Content-Type"] = "plain/text";
                await Response.WriteAsync("Latest image was found, but unable to download image for unknown reason.");
                return;
            }

            //Write into response
            Response.StatusCode = 200;
            Response.Headers["Content-Type"] = "image/jpeg";
            await ii.Image.CopyToAsync(Response.Body);
        }

    }
}