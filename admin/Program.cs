using System;
using Spectre.Console;
using Azure.Storage.Blobs;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;

namespace CMonitorAdministration
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainProgramAsync().Wait();
        }

        public async static Task MainProgramAsync()
        {
            SelectionPrompt<string> ToDo = new SelectionPrompt<string>();
            ToDo.Title("What do you want to do?");
            ToDo.AddChoice("Download images between a date range");

            //Ask
            string WantToDo = AnsiConsole.Prompt(ToDo);
            if (WantToDo == "Download images between a date range")
            {
                //Authenticate w/ azure blob storage
                AnsiConsole.Markup("[italic]Setting up blob storage...[/] ");
                BlobServiceClient bsc = new BlobServiceClient(GetAzureBlobStorageConnectionString());
                BlobContainerClient bcc = bsc.GetBlobContainerClient("cmonitor-images");
                if (await bcc.ExistsAsync() == false)
                {
                    await bcc.CreateAsync();
                }
                AnsiConsole.MarkupLine("[green]set up![/]");

                //Get begin date?
                DateTime starting = DateTime.UtcNow.AddDays(-7); //default to 1 week
                Console.Write("Starting when? (in MM/DD/YYYY format) > ");
                string? sw = Console.ReadLine();
                if (sw != null)
                {
                    starting = DateTime.Parse(sw);
                }

                //Get end date
                DateTime ending = DateTime.UtcNow; //default to now
                Console.Write("Ending when? (in MM/DD/YYYY format) > ");
                string? ew = Console.ReadLine();
                if (ew != null)
                {
                    ending = DateTime.Parse(ew);
                }

                //Get in between dates
                DateTime[] AllDatesToGet = GetDatesBetween(starting, ending);
                AnsiConsole.MarkupLine("Going to download [bold]" + AllDatesToGet.Length.ToString("#,##0") + "[/] days worth of photos:");
                foreach (DateTime dt in AllDatesToGet)
                {
                    Console.WriteLine("- " + dt.ToShortDateString());
                }

                

                //Get list of all photos
                List<string> PhotosToDownload = new List<string>();
                Console.WriteLine();
                foreach (DateTime dt in AllDatesToGet)
                {
                    AnsiConsole.Markup("Querying photos for [bold][navy]" + dt.ToShortDateString() + "[/][/]... ");
                    Azure.Pageable<BlobItem> items = bcc.GetBlobs(prefix: dt.Year.ToString("0000") + dt.Month.ToString("00") + dt.Day.ToString("00"));
                    AnsiConsole.MarkupLine("[bold]" + items.Count().ToString("#,##0") + "[/] photos found");
                    foreach (BlobItem bi in items)
                    {
                        PhotosToDownload.Add(bi.Name);
                    }
                }

                //Ready!
                Console.WriteLine();
                AnsiConsole.Markup("[italic][gray]Ready to download [bold][navy]" + PhotosToDownload.Count.ToString("#,##0") + " photos[/][/]when you are. Enter to proceed.[/][/]");
                Console.ReadLine();

                //Make a folder for us to download in
                string DownloadPath = Path.Combine("./", "download-" + Guid.NewGuid().ToString().Replace("-", ""));
                System.IO.Directory.CreateDirectory(DownloadPath);

                //Download all
                AnsiConsole.MarkupLine("Proceeding to download [bold][navy]" + PhotosToDownload.Count.ToString("#,##0") + "[/][/] over [bold][navy]" + AllDatesToGet.Length.ToString("#,##0") + "[/][/] dates!");
                for (int t = 0; t < PhotosToDownload.Count; t++)
                {    
                    string blobname = PhotosToDownload[t];
                    string destpath = Path.Combine(DownloadPath, blobname);
                    FileStream fs = System.IO.File.Create(destpath); //Create the file
                    fs.Close();
                    AnsiConsole.Markup("Downloading [bold][navy]" + blobname + "[/][/]... ");
                    BlobClient bc = bcc.GetBlobClient(blobname);
                    if (await bc.ExistsAsync() == false)
                    {
                        continue;
                    }
                    await bc.DownloadToAsync(destpath);
                    AnsiConsole.Markup("[green]Downloaded![/]");
                }
            }
        }




        //Retrieves azure blob storage connection string from azblobconstr.txt in parent directory
        public static string GetAzureBlobStorageConnectionString()
        {
            string path = "../azblobconstr.txt";
            if (System.IO.File.Exists(path) == false)
            {
                throw new Exception("Unable to retrieve Azure Blob Storage connection string: cannot find file it should be in!");
            }
            else
            {
                return System.IO.File.ReadAllText(path);
            }
        }

        public static DateTime[] GetDatesBetween(DateTime start, DateTime end)
        {
            List<DateTime> dates = new List<DateTime>();
            for (DateTime date = start; date <= end; date = date.AddDays(1))
            {
                dates.Add(date);
            }
            return dates.ToArray();
        }



    }
}