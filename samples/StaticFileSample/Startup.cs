using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StaticFilesSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDirectoryBrowser();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory factory, IHostingEnvironment host)
        {
            Console.WriteLine("webroot: " + host.WebRootPath);

            // Displays all log levels
            factory.AddConsole(LogLevel.Debug);

            var options = new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
            };
            options.StaticFileOptions.CacheProfile = new CacheProfile
            {
                Duration = TimeSpan.FromSeconds(30),
                Location = ResponseCacheLocation.Client,
            };
            options.StaticFileOptions.CacheProfileProvider =
                ctx =>
                {
                    if (ctx.File.Name.EndsWith(".html"))
                    {
                        return new CacheProfile
                        {
                            Duration = TimeSpan.FromMinutes(15),
                            Location = ResponseCacheLocation.Any,
                        };
                    }
                    //use default CacheProfile
                    return null;
                };
            app.UseFileServer(options);
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
