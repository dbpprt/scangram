using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Scangram.Common;
using Scangram.Common.DocumentDetection;
using Scangram.Common.DocumentDetection.Contracts;
using Scangram.Common.DocumentDetection.Detectors;
using Scangram.Common.DocumentDetection.Preprocessors;
using Scangram.Common.DocumentDetection.Scorer;
using Scangram.Services;
using Scangram.Services.Contracts;
using Telegram.Bot;

namespace Scangram
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IOptions<ApplicationConfiguration> _applicationConfiguration;

        public Startup(IConfiguration configuration, IOptions<ApplicationConfiguration> applicationConfiguration)
        {
            _configuration = configuration;
            _applicationConfiguration = applicationConfiguration;
        }
        
        [UsedImplicitly]
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();

            var bot = new TelegramBotClient(_applicationConfiguration.Value.TelegramAccessToken);
            services.AddSingleton<ITelegramBotClient>(bot);

            services.AddSingleton(new ImageDocumentDetectionService(
                new IImagePreProcessor[] { new ResizeImagePreProcessor(1600), new SimpleCannyImagePreProcessor() },
                new SimplePerspectiveTransformImageExtractor(),
                new IContourDetector[] { new SimpleContourDetector(20, 0.005), new SimpleContourDetector(15, 0.03), new ConvexHullContourDetector(10, 0.03) },
                new IResultScorer[] { new FourEdgesScorer(), new ConvexityScorer(), new AreaScorer(10), new HoughLinesScorer() },
                new IImagePostProcessor[] { }));

            services.AddSingleton<IHostedService, TelegramBotHostedService>();
            services.AddSingleton<IWorkerService, WorkerService>();
            services.AddSingleton<IHostedService>(_ => (HostedService)_.GetService<IWorkerService>());
            
            services.AddMvc();
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        public static void ConfigureStartupServices(WebHostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;

            // map all configuration sections
            services.Configure<ApplicationConfiguration>(
                configuration.GetSection("Configuration"));
        }
    }
}
