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
            while (true)
            {
                //Set up ask
                Console.Clear();
                SelectionPrompt<string> ToDo = new SelectionPrompt<string>();
                AnsiConsole.MarkupLine("[bold][underline]Welcome to CMonitor Admin Application![/][/]");
                ToDo.Title("What do you want to do?");
                ToDo.AddChoice("Download images between a date range");
                ToDo.AddChoice("Check most recent image upload");
                ToDo.AddChoice("Exit");

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
                    Console.WriteLine();
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
                    AnsiConsole.Markup("[italic][gray]Ready to download [navy]" + PhotosToDownload.Count.ToString("#,##0") + " photos[/] when you are. Enter to proceed.[/][/]");
                    Console.ReadLine();

                    //Make a folder for us to download in
                    string DownloadPath = Path.Combine("download-" + Guid.NewGuid().ToString().Replace("-", ""));
                    System.IO.Directory.CreateDirectory(DownloadPath);

                    //Download all
                    Console.WriteLine();
                    AnsiConsole.MarkupLine("Proceeding to download [bold][navy]" + PhotosToDownload.Count.ToString("#,##0") + "[/][/] over [bold][navy]" + AllDatesToGet.Length.ToString("#,##0") + "[/][/] dates!");
                    for (int t = 0; t < PhotosToDownload.Count; t++)
                    {    

                        //Print what we're doing
                        string blobname = PhotosToDownload[t];
                        float percent = Convert.ToSingle(t) / Convert.ToSingle(PhotosToDownload.Count);
                        AnsiConsole.Markup("[gray](" + t.ToString("#,##0") + " / " + PhotosToDownload.Count.ToString("#,##0") + ", " + percent.ToString("#0.0%") + ")[/]" + " Downloading [bold][navy]" + blobname + "[/][/]... ");
                        
                        //Download!
                        string destpath = Path.Combine(DownloadPath, blobname);
                        FileStream fs = System.IO.File.Create(destpath); //Create the file
                        fs.Close();
                        BlobClient bc = bcc.GetBlobClient(blobname);
                        if (await bc.ExistsAsync() == false)
                        {
                            continue;
                        }
                        await bc.DownloadToAsync(destpath);
                        AnsiConsole.MarkupLine("[green]Downloaded![/]");
                    }

                    //Print
                    Console.WriteLine();
                    AnsiConsole.MarkupLine("[green]" + PhotosToDownload.Count.ToString("#,##0") + " photos downloaded to '" + System.IO.Path.GetFullPath(DownloadPath) + "'![/]");
                    Console.WriteLine();

                    //Do you also want to rename them in order of oldest to newest (i.e. "0000001", "0000002", "0000003", etc.)
                    SelectionPrompt<string> RenameOption = new SelectionPrompt<string>();
                    RenameOption.Title("Do you also want to rename them in order of oldest to newest (i.e. '0000001', '0000002', '0000003', etc.)");
                    RenameOption.AddChoice("Yes");
                    RenameOption.AddChoice("No");
                    string RenameOptionSelection = AnsiConsole.Prompt(RenameOption);
                    if (RenameOptionSelection == "Yes")
                    {
                        //Get all files
                        string[] allfiles = System.IO.Directory.GetFiles(DownloadPath);

                        //Construct dict of datetimes
                        Dictionary<string, DateTime> FileDateTimes = new Dictionary<string, DateTime>();
                        foreach (string file in allfiles)
                        {
                            string name = System.IO.Path.GetFileNameWithoutExtension(file);
                            DateTime ts = TimeStamper.TimeStampToDateTime(name);
                            FileDateTimes[file] = ts;
                        }

                        //Arrange in order from oldest to newest
                        List<string> FilesToRename = new List<string>(); // In order from oldest to newest
                        while (FileDateTimes.Count > 0)
                        {
                            KeyValuePair<string, DateTime> winner = FileDateTimes.First(); //the oldest
                            foreach (KeyValuePair<string, DateTime> kvp in FileDateTimes)
                            {
                                if (kvp.Value < winner.Value)
                                {
                                    winner = kvp;
                                }
                            }
                            FilesToRename.Add(winner.Key); //Add it
                            FileDateTimes.Remove(winner.Key); //Remove
                        }

                        //Rename each!
                        int ticker = 0;
                        foreach (string file in FilesToRename)
                        {
                            string? dir = System.IO.Path.GetDirectoryName(file); //Get the parent directory path
                            if (dir != null)
                            {
                                string path_old = file;
                                string path_new = Path.Combine(dir, ticker.ToString("0000000#") + ".jpg");
                                AnsiConsole.Markup("Renaming '" + path_old + "' to '" + path_new + "'... ");
                                System.IO.File.Move(path_old, path_new); //rename
                                AnsiConsole.MarkupLine("Success!");
                                ticker = ticker + 1;
                            }
                        }

                        Console.WriteLine();
                    }      
                }
                else if (WantToDo == "Check most recent image upload")
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

                    //Begin search
                    DateTime FurthestSearch = DateTime.UtcNow.AddDays(-7);
                    DateTime[] ToSearch = GetDatesBetween(FurthestSearch, DateTime.UtcNow);
                    bool answered = false;
                    for (int t = 0; t < ToSearch.Length; t++)
                    {
                        if (answered == false)
                        {
                            DateTime ToSearchNow = ToSearch[ToSearch.Length - 1 - t]; //Inverse direction
                            AnsiConsole.Markup("Searching on date [bold]" + ToSearchNow.ToShortDateString() + "[/]... ");
                            Azure.Pageable<BlobItem> items = bcc.GetBlobs(prefix: ToSearchNow.Year.ToString("0000") + ToSearchNow.Month.ToString("00") + ToSearchNow.Day.ToString("00")); //Search on the date (using prefix)
                            AnsiConsole.MarkupLine("[bold]" + items.Count().ToString("#,##0") + "[/] photos found");
                            
                            //If there are items, get the most recent one!
                            if (items.Count() > 0)
                            {
                                //Get the most recent
                                DateTime MostRecent = new DateTime(1900, 1, 1); //Default date (a long time ago so every date is more recent than this!)
                                foreach (BlobItem item in items)
                                {
                                    string timestamp = item.Name.Replace(".jpg", "");
                                    DateTime thisstamp = TimeStamper.TimeStampToDateTime(timestamp);
                                    if (thisstamp > MostRecent)
                                    {
                                        MostRecent = thisstamp;
                                    }
                                }

                                //Return the most recent
                                TimeSpan ts = DateTime.UtcNow - MostRecent;
                                AnsiConsole.MarkupLine("The last picture that was taken was taken at [bold]" + MostRecent.ToString() + "[/] UTC");
                                AnsiConsole.MarkupLine("That was [bold]" + ts.TotalMinutes.ToString("#,##0") + " minute(s) ago![/]");
                                Console.WriteLine();
                                answered = true; //Mark as answered so other loops know to ignore!
                            }
                        }
                    }

                    //If still not answered, say it was before the oldest time we checked
                    if (answered == false)
                    {
                        AnsiConsole.MarkupLine("[red]I looked as far back as " + ToSearch[0].ToShortDateString() + " and was unable to find a photo taken! The most recent photo was likely taken before then.[/]");
                    }
                }
                else if (WantToDo == "Exit")
                {
                    Console.WriteLine();
                    AnsiConsole.MarkupLine("[bold]Bye bye![/]");
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine("I don't know what that is!");
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