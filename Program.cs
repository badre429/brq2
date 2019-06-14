using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GeoMapDownloader
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>().UseKestrel((context, options) =>
                     {
                         var port = Environment.GetEnvironmentVariable("PORT");
                         if (string.IsNullOrWhiteSpace(port))
                         {
                             port = "5000";
                         }
                         if (!string.IsNullOrEmpty(port))
                         {
                             options.ListenAnyIP(int.Parse(port));
                         }
                     });
                });

        //       public static IWebHostBuilder CreateHostBuilder(string[] args) =>
        // WebHost.CreateDefaultBuilder(args)
        //     .UseStartup<Startup>()
        //      .UseKestrel((context, options) =>
        //     {
        //         var port = Environment.GetEnvironmentVariable("PORT");
        //         if (!string.IsNullOrEmpty(port))
        //         {
        //             options.ListenAnyIP(int.Parse(port));
        //         }
        //     });
    }
}
