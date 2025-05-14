using System;

namespace CamMonitorAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Set up builder
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls("http://0.0.0.0:80");
            builder.Services.AddControllers();

            //Add azure blob storage connection string as datastore
            string path = "../azblobconstr.txt";
            string constr = System.IO.File.ReadAllText(path);
            Console.WriteLine("Constr: " + constr);
            BlobConStr bcs = new BlobConStr(constr);
            builder.Services.AddSingleton(bcs);

            //Run the app
            var app = builder.Build();
            app.MapControllers();
            app.Run();
        }
    }
}