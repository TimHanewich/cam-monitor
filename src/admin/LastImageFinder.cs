using System;
using Azure.Storage.Blobs;
using System.Collections.Generic;
using Azure.Storage.Blobs.Models;

namespace CMonitorAdministration
{
    public class LastImageFinder
    {
        private string _constr;

        public LastImageFinder(string azure_blob_connection_string)
        {
            _constr = azure_blob_connection_string;
        }

        public ImageInfo FindLastImage(string container_name, bool download = false, int days = 7)
        {
            BlobServiceClient bsc = new BlobServiceClient(_constr);
            BlobContainerClient bcc = bsc.GetBlobContainerClient(container_name);

            //Compile a list of days we will check on
            List<DateTime> DaysToSearch = new List<DateTime>();
            for (int i = 0; i < days; i++)
            {
                DaysToSearch.Add(DateTime.UtcNow.AddDays(i * -1));
            }

            //Search
            string? LastName = null; // name of the blob
            DateTime LastDateTime = new DateTime(1900, 1, 1); //datetime
            foreach (DateTime DayToSearch in DaysToSearch)
            {
                if (LastName == null)
                {
                    string prefix = DayToSearch.Year.ToString("0000") + DayToSearch.Month.ToString("00") + DayToSearch.Day.ToString("00");
                    Azure.Pageable<BlobItem> PicsOnDay = bcc.GetBlobs(prefix: prefix);
                    foreach (BlobItem bi in PicsOnDay)
                    {
                        int PeriodLocation = bi.Name.IndexOf(".");
                        if (PeriodLocation != -1)
                        {
                            string name = bi.Name.Substring(0, PeriodLocation);
                            DateTime dt = TimeStamper.TimeStampToDateTime(name);
                            if (dt > LastDateTime)
                            {
                                LastDateTime = dt;
                                LastName = bi.Name;
                            }
                        }
                    }
                }
            }

            //Fail if nothing was found
            if (LastName == null)
            {
                throw new Exception("Unable to find a valid picture file for the last " + days.ToString() + " days!");
            }

            //Assemble the found last file
            ImageInfo ToReturn = new ImageInfo();
            ToReturn.Name = LastName;
            ToReturn.CapturedAtUtc = LastDateTime;

            //Download it?
            if (download)
            {
                BlobClient bc = bcc.GetBlobClient(ToReturn.Name);
                ToReturn.Image = new MemoryStream();
                bc.DownloadTo(ToReturn.Image);
                ToReturn.Image.Position = 0;
            }

            return ToReturn;
        }

    }
}