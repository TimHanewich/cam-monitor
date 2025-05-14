using System;

namespace CamMonitorAPI
{
    public class BlobConStr
    {
        public string AzureBlobConnectionString {get;}

        public BlobConStr(string constr)
        {
            AzureBlobConnectionString = constr;
        }
    }
}