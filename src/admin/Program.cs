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
                SelectionPrompt<string> ToDo = new SelectionPrompt<string>();
                AnsiConsole.MarkupLine("[bold][underline]Welcome to CMonitor Admin Application![/][/]");
                ToDo.Title("What do you want to do?");
                ToDo.AddChoice("Download images between a date range");
                ToDo.AddChoice("Download images by prefix");
                ToDo.AddChoice("Check most recent image upload");
                ToDo.AddChoice("Exit");

                //Ask
                string WantToDo = AnsiConsole.Prompt(ToDo);
                if (WantToDo == "Download images between a date range")
                {
                    //Authenticate w/ azure blob storage
                    AnsiConsole.Markup("[italic]Setting up blob storage...[/] ");
                    BlobServiceClient bsc = new BlobServiceClient(GetAzureBlobStorageConnectionString());
                    AnsiConsole.MarkupLine("set up!");

                    //List out containers to check
                    Azure.Pageable<BlobContainerItem> containers = bsc.GetBlobContainers();
                    SelectionPrompt<string> ContainerToSearchPrompt = new SelectionPrompt<string>();
                    ContainerToSearchPrompt.Title("What container do you want to search?");
                    foreach (BlobContainerItem bci in containers)
                    {
                        ContainerToSearchPrompt.AddChoice(bci.Name);
                    }
                    string ContainerToSearch = AnsiConsole.Prompt(ContainerToSearchPrompt);

                    //Get the container
                    BlobContainerClient bcc = bsc.GetBlobContainerClient(ContainerToSearch);
                    AnsiConsole.MarkupLine("Great, I will download images from [bold]" + ContainerToSearch + "[/]."); 

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

                    //Download
                    await DownloadPhotosAsync(bcc, PhotosToDownload.ToArray());
                }
                else if (WantToDo == "Download images by prefix")
                {
                    //Authenticate w/ azure blob storage
                    AnsiConsole.Markup("[italic]Setting up blob storage...[/] ");
                    BlobServiceClient bsc = new BlobServiceClient(GetAzureBlobStorageConnectionString());
                    AnsiConsole.MarkupLine("set up!");

                    //List out containers to check
                    Azure.Pageable<BlobContainerItem> containers = bsc.GetBlobContainers();
                    SelectionPrompt<string> ContainerToSearchPrompt = new SelectionPrompt<string>();
                    ContainerToSearchPrompt.Title("What container do you want to search?");
                    foreach (BlobContainerItem bci in containers)
                    {
                        ContainerToSearchPrompt.AddChoice(bci.Name);
                    }
                    string ContainerToSearch = AnsiConsole.Prompt(ContainerToSearchPrompt);

                    //Get the container
                    BlobContainerClient bcc = bsc.GetBlobContainerClient(ContainerToSearch);
                    AnsiConsole.MarkupLine("Great, I will download images from [bold]" + ContainerToSearch + "[/]."); 

                    //Ask for the prefix.
                    string prefix = AnsiConsole.Ask<string>("What is the prefix?");

                    //Get list of all photos
                    List<string> PhotosToDownload = new List<string>();
                    AnsiConsole.Markup("Querying photos that start with [bold][navy]" + prefix + "[/][/]... ");
                    Azure.Pageable<BlobItem> items = bcc.GetBlobs(prefix: prefix);
                    AnsiConsole.MarkupLine("[bold]" + items.Count().ToString("#,##0") + "[/] photos found");
                    foreach (BlobItem bi in items)
                    {
                        PhotosToDownload.Add(bi.Name);
                    }

                    //Download
                    await DownloadPhotosAsync(bcc, PhotosToDownload.ToArray());
                }
                else if (WantToDo == "Check most recent image upload")
                {
                    //Authenticate w/ azure blob storage
                    AnsiConsole.Markup("[italic]Setting up blob storage...[/] ");
                    BlobServiceClient bsc = new BlobServiceClient(GetAzureBlobStorageConnectionString());
                    AnsiConsole.MarkupLine("set up!");

                    //List out containers to check
                    Azure.Pageable<BlobContainerItem> containers = bsc.GetBlobContainers();
                    SelectionPrompt<string> ContainerToSearchPrompt = new SelectionPrompt<string>();
                    ContainerToSearchPrompt.Title("What container do you want to search?");
                    foreach (BlobContainerItem bci in containers)
                    {
                        ContainerToSearchPrompt.AddChoice(bci.Name);
                    }
                    string ContainerToSearch = AnsiConsole.Prompt(ContainerToSearchPrompt);

                    //Get the container
                    BlobContainerClient bcc = bsc.GetBlobContainerClient(ContainerToSearch);
                    AnsiConsole.MarkupLine("Great, I will search container [bold]" + ContainerToSearch + "[/] for the most recent image upload."); 

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
                            string prefix = ToSearchNow.Year.ToString("0000") + ToSearchNow.Month.ToString("00") + ToSearchNow.Day.ToString("00");
                            Azure.Pageable<BlobItem> items = bcc.GetBlobs(prefix: prefix); //Search on the date (using prefix)
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
                                if (ts.TotalMinutes <= 1)
                                {
                                    AnsiConsole.MarkupLine("That was [bold]" + ts.TotalSeconds.ToString("#,##0") + " seconds ago![/]");
                                }
                                else // 2 or more minutes
                                {
                                    AnsiConsole.MarkupLine("That was [bold]" + ts.TotalMinutes.ToString("#,##0") + " minute(s) ago![/]");
                                }
                                
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

                //Enter to continue
                Console.WriteLine();
                AnsiConsole.Markup("[gray][italic]Enter to continue...[/][/]");
                Console.ReadLine();
                Console.Clear();
            }
        }

        //DOWNLOAD
        public static async Task DownloadPhotosAsync(BlobContainerClient bcc, string[] names)
        {
            //Ready!
            Console.WriteLine();
            AnsiConsole.Markup("[italic][gray]Ready to download [navy]" + names.Length.ToString("#,##0") + " photos[/] when you are. Enter to proceed.[/][/]");
            Console.ReadLine();

            //Make a folder for us to download in
            string DownloadPath = Path.Combine("download-" + Guid.NewGuid().ToString().Replace("-", ""));
            System.IO.Directory.CreateDirectory(DownloadPath);

            //Download all
            Console.WriteLine();
            AnsiConsole.MarkupLine("Proceeding to download [bold][navy]" + names.Length.ToString("#,##0") + "[/][/] photos!");
            int BytesDownloaded = 0;
            for (int t = 0; t < names.Length; t++)
            {    

                //Print what we're doing
                string blobname = names[t];
                float percent = Convert.ToSingle(t) / Convert.ToSingle(names.Length);
                AnsiConsole.Markup("[gray](" + t.ToString("#,##0") + " / " + names.Length.ToString("#,##0") + ", " + percent.ToString("#0.0%") + ")[/]" + " Downloading [bold][navy]" + blobname + "[/][/]... ");
                
                //Download into memory stream
                BlobClient bc = bcc.GetBlobClient(blobname);
                MemoryStream ms = new MemoryStream();
                await bc.DownloadToAsync(ms);
                BytesDownloaded = BytesDownloaded + Convert.ToInt32(ms.Length);

                //Write into file
                string destpath = Path.Combine(DownloadPath, blobname);
                FileStream fs = System.IO.File.Create(destpath); //Create the file
                fs.Position = 0;
                ms.Position = 0;
                ms.CopyTo(fs);
                fs.Close();
                ms.Close();
                await bc.DownloadToAsync(destpath);
                AnsiConsole.MarkupLine("[green]Downloaded![/]");
            }

            //Print
            float mb_downloaded = Convert.ToSingle(BytesDownloaded) / Convert.ToSingle(1048576);
            Console.WriteLine();
            AnsiConsole.MarkupLine("[green]" + names.Length.ToString("#,##0") + " photos downloaded to '" + System.IO.Path.GetFullPath(DownloadPath) + "'![/]");
            AnsiConsole.MarkupLine("[green]" + mb_downloaded.ToString("#,##0.0") + " MB downloaded![/]");
            Console.WriteLine();

            //Do you also want to rename them in order of oldest to newest (i.e. "0000001", "0000002", "0000003", etc.)
            SelectionPrompt<string> RenameOption = new SelectionPrompt<string>();
            RenameOption.Title("Do you also want to rename them in order of oldest to newest (i.e. '0000001', '0000002', '0000003', etc.)");
            RenameOption.AddChoice("Yes");
            RenameOption.AddChoice("No");
            string RenameOptionSelection = AnsiConsole.Prompt(RenameOption);
            if (RenameOptionSelection == "Yes")
            {
                RenameSequential(DownloadPath);
                Console.WriteLine();
            }
        }


        //Upload all
        public static async Task UploadAllFromFolderAsync(string folder)
        {
            string[] files = System.IO.Directory.GetFiles(folder);
            Console.WriteLine("Found " + files.Length.ToString("#,##0") + " files in this folder!");

            //Authenticate w/ azure blob storage
            AnsiConsole.Markup("[italic]Setting up blob storage...[/] ");
            BlobServiceClient bsc = new BlobServiceClient(GetAzureBlobStorageConnectionString());
            BlobContainerClient bcc = bsc.GetBlobContainerClient("cmonitor-images");
            if (await bcc.ExistsAsync() == false)
            {
                await bcc.CreateAsync();
            }
            AnsiConsole.MarkupLine("[green]set up![/]");

            //Upload each
            for (int t = 0; t < files.Length; t++)
            {
                string path = files[t];
                string name = System.IO.Path.GetFileName(path);

                //Get blob
                BlobClient bc = bcc.GetBlobClient(name);
                if (bc.Exists() == false)
                {
                    float percent_complete = Convert.ToSingle(t) / Convert.ToSingle(files.Length);
                    Console.Write(percent_complete.ToString("#0.0%") + "% - " + "Uploading '" + name + "'... ");
                    Stream s = System.IO.File.OpenRead(path);
                    await bc.UploadAsync(s);
                    s.Close();
                    Console.WriteLine("Success!");
                }
                else
                {
                    Console.WriteLine("Blob '" + name + "' already exists!");
                }
            }
        }

        //Rename all from a datetime stamp to number-based (i.e. 00000001.jpg, 00000002.jpg, etc.)
        public static void RenameSequential(string folder_path)
        {
            //Get all files
            AnsiConsole.Markup("[italic]Reading files... [/]");
            string[] allfiles = System.IO.Directory.GetFiles(folder_path);
            AnsiConsole.MarkupLine("[italic]" + allfiles.Length.ToString("#,##0") +  " files listed.[/]");

            //Construct dict of datetimes
            AnsiConsole.Markup("[italic]Parsing timestamps from files... [/]");
            Dictionary<string, DateTime> FileDateTimes = new Dictionary<string, DateTime>();
            foreach (string file in allfiles)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(file);
                DateTime ts = TimeStamper.TimeStampToDateTime(name);
                FileDateTimes[file] = ts;
            }
            AnsiConsole.MarkupLine("[italic]done![/]");

            //Arrange in order from oldest to newest
            AnsiConsole.Markup("[italic]Arranging in order... [/]");
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
            AnsiConsole.MarkupLine("[italic]done![/]");

            //Rename each!
            int ticker = 0;
            foreach (string file in FilesToRename)
            {
                string? dir = System.IO.Path.GetDirectoryName(file); //Get the parent directory path
                if (dir != null)
                {
                    float percent_complete = Convert.ToSingle(ticker) / Convert.ToSingle(FilesToRename.Count);
                    string path_old = file;
                    string path_new = Path.Combine(dir, ticker.ToString("0000000#") + ".jpg");
                    AnsiConsole.Markup("[gray](" + percent_complete.ToString("#0.0%") + ")[/] Renaming '" + path_old + "' to '" + path_new + "'... ");
                    System.IO.File.Move(path_old, path_new); //rename
                    AnsiConsole.MarkupLine("Success!");
                    ticker = ticker + 1;
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