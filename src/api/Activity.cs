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
    [Route("activity")]
    public class Activity : ControllerBase
    {
        private BlobConStr _bcs;
        public Activity(BlobConStr bcs)
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
                ii = lif.FindLastImage("camera1", false, 7);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 404;
                Response.Headers["Content-Type"] = "plain/text";
                await Response.WriteAsync("There was a fatal error while trying to find the last image! Message: " + ex.Message);
                return;
            }

            //Draft resposne
            int secondsAgo = Convert.ToInt32((DateTime.UtcNow - ii.CapturedAtUtc).TotalSeconds);
            JObject ToReturn = new JObject();
            ToReturn.Add("secondsAgo", secondsAgo);

            //Respond
            Response.StatusCode = 200;
            Response.Headers["Content-Type"] = "application/json";
            await Response.WriteAsync(ToReturn.ToString());
        }

    }
}