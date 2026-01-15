using MagazineFetcher.Classification;
using MagazineFetcher.Clients;
using MagazineFetcher.Extraction;
using MagazineFetcher.Hosting;
using MagazineFetcher.Rss;
using MagazineFetcher.Torrents;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NLog.Extensions.Logging;

namespace MagazineFetcher
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			await Host.CreateDefaultBuilder(args)
				.ConfigureAppConfiguration(cfg =>
				{
					cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
					cfg.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
					cfg.AddCommandLine(args);
				})
				.ConfigureServices((hostContext, services) =>
				{
					services.AddHostedService<ConsoleHostedService>();
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
				.RunConsoleAsync();

		}
	}
}
