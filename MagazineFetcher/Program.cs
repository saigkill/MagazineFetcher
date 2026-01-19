using MagazineFetcher.AppConfig;
using MagazineFetcher.Classification;
using MagazineFetcher.Clients;
using MagazineFetcher.Extraction;
using MagazineFetcher.Hosting;
using MagazineFetcher.Rss;
using MagazineFetcher.Torrents;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NLog.Extensions.Logging;

namespace MagazineFetcher
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			await Host.CreateDefaultBuilder(args)
				.ConfigureAppConfiguration((context, services) =>
				{
					services.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
					services.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

					if (context.HostingEnvironment.IsDevelopment())
					{
						services.AddUserSecrets<Program>();
					}
					services.AddCommandLine(args);
				})
				.ConfigureServices((hostContext, services) =>
				{
					services.AddHostedService<ConsoleHostedService>();
					services.AddOptions<Configuration>()
						.Bind(hostContext.Configuration.GetSection("Configuration"))
						.ValidateDataAnnotations()
						.ValidateOnStart();
					services.AddSingleton<Hosting.MagazineFetcher>();
					services.AddSingleton<TorrentHistory>();
					services.AddSingleton<RssFetcher>();
					services.AddSingleton<TorrentDownloader>();
					services.AddSingleton<ArchiveExtractor>();
					services.AddSingleton<MagazineClassifier>();
					services.AddSingleton<FileRenamer>();
					services.AddSingleton<QBittorrentClient>();
					services.AddSingleton<DownloadWatcher>();
					services.AddNLog();

				})
				.ConfigureLogging(logging =>
				{
					logging.ClearProviders();

				})
				.RunConsoleAsync();
		}
	}
}
