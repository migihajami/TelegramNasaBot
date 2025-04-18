using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Serilog;
using System;
using Telegram.Bot;
using TelegramNasaBot.Interfaces;
using TelegramNasaBot.Models;
using TelegramNasaBot.Services;

namespace TelegramNasaBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .Build();

            // Set up Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                Log.Information("Starting Telegram NASA Bot...");

                // Set up DI
                var services = new ServiceCollection();
                ConfigureServices(services, configuration);
                var serviceProvider = services.BuildServiceProvider();

                // Set up Quartz scheduler with DI
                var schedulerFactory = new StdSchedulerFactory();
                var scheduler = await schedulerFactory.GetScheduler();
                scheduler.JobFactory = new MicrosoftDependencyInjectionJobFactory(serviceProvider);
                services.AddSingleton(scheduler);

                // Define the job
                var job = JobBuilder.Create<NasaPhotoJob>()
                    .WithIdentity("NasaPhotoJob", "NasaGroup")
                    .Build();

                // Define the trigger (daily at 12:10 GMT)
                var trigger = TriggerBuilder.Create()
                    .WithIdentity("NasaPhotoTrigger", "NasaGroup")
                    .WithCronSchedule("0 10 12 * * ?", x => x.InTimeZone(TimeZoneInfo.Utc)) // 12:10 UTC
                    //.WithCronSchedule("0 * * * * ?", x => x.InTimeZone(TimeZoneInfo.Utc)) // Uncomment for testing every minute
                    .Build();

                // Schedule the job
                await scheduler.ScheduleJob(job, trigger);
                Log.Information("Scheduled NASA photo job to run daily at 12:10 GMT.");

                // Start the scheduler
                await scheduler.Start();

                Log.Information("Bot is running.");
                await Task.Delay(-1); // Keep running
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Bot terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Add configurations
            services.Configure<TelegramSettings>(configuration.GetSection("Telegram"));
            services.Configure<NasaSettings>(configuration.GetSection("Nasa"));
            services.AddSingleton(configuration.GetSection("Telegram").Get<TelegramSettings>());

            // Add services
            services.AddHttpClient<IPhotoFetcher, PhotoFetcher>();
            services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();
            services.AddSingleton<IPublisher, Publisher>();
            services.AddSingleton<ILogger>(Log.Logger);
            services.AddSingleton<ITelegramBotClient>(sp =>
                new TelegramBotClient(configuration.GetSection("Telegram:BotToken").Value));

            // Add Quartz
            services.AddSingleton<NasaPhotoJob>();
        }
    }

    // Custom job factory to use DI
    public class MicrosoftDependencyInjectionJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public MicrosoftDependencyInjectionJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return _serviceProvider.GetService(bundle.JobDetail.JobType) as IJob;
        }

        public void ReturnJob(IJob job) { }
    }
}