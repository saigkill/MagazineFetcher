using Microsoft.Extensions.Hosting;

using System.IO;
using System.Text.Json;

using MagazineFetcher.AppConfig;
using MagazineFetcher.Classification;
using MagazineFetcher.Clients;
using MagazineFetcher.Extraction;
using MagazineFetcher.Hosting;
using MagazineFetcher.Rss;
using MagazineFetcher.Torrents;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;

namespace MagazineFetcher
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			await Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseKestrel();
					webBuilder.ConfigureAppConfiguration((hostingContext, config) =>
					{
						// nichts nötig – nur damit wir hostingContext haben
					});

					webBuilder.Configure((context, app) =>
					{
						var port = context.Configuration["Configuration:WebUiPort"] ?? "8080";

						// Korrekt: Adressen über IServerAddressesFeature setzen
						var addressesFeature = app.ServerFeatures.Get<IServerAddressesFeature>();
						if (addressesFeature != null)
						{
							// optional vorhandene Einträge entfernen
							addressesFeature.Addresses.Clear();
							addressesFeature.Addresses.Add($"http://*:{port}");
						}

						app.UseStaticFiles();
						app.UseRouting();

						app.UseEndpoints(endpoints =>
						{
							endpoints.MapGet("/api/config",
								(IOptions<MagazineFetcherOptions> options) =>
									Results.Json(options.Value));

							endpoints.MapPost("/api/config",
								async (MagazineFetcherOptions newConfig) =>
								{
									var json = JsonSerializer.Serialize(newConfig,
										new JsonSerializerOptions { WriteIndented = true });

									await File.WriteAllTextAsync(
										"/share/CACHEDEV1_DATA/.qpkg/MagazineFetcher/config.json", json);

									return Results.Ok();
								});
						});
					});
				})
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
					var logDir = hostContext.Configuration["Configuration:Logging:LogDirectory"];
					NLog.LogManager.Configuration.Variables["logDir"] = logDir;

					services.AddHostedService<ConsoleHostedService>();
					services.AddOptions<MagazineFetcherOptions>()
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
				})
				.ConfigureLogging(logging =>
				{
					logging.ClearProviders();
					logging.AddNLog();
				})
				.RunConsoleAsync();
		}
	}
}
