using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Scangram
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(Startup.ConfigureStartupServices)
                .UseStartup<Startup>()
                .Build();
    }
}
