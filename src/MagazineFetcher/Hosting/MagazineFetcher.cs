// <copyright file="MagazineFetcher.cs" company="Sascha Manns">
// Copyright (c) 2026 Sascha Manns.
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the “Software”), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>

using MagazineFetcher.AppConfig;
using MagazineFetcher.Classification;
using MagazineFetcher.Extraction;
using MagazineFetcher.Rss;
using MagazineFetcher.Torrents;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NLog;

using QBittorrentClient = MagazineFetcher.Clients.QBittorrentClient;

namespace MagazineFetcher.Hosting;

public class MagazineFetcher()
{
	public IOptions<MagazineFetcherOptions> Configuration { get; set; }
	public ILogger<MagazineFetcher> Logger { get; set; }
	public RssFetcher RssFetcher { get; set; }
	public TorrentHistory TorrentHistory { get; set; }
	public TorrentDownloader TorrentDownloader { get; set; }
	public ArchiveExtractor ArchiveExtractor { get; set; }
	public MagazineClassifier MagazineClassifier { get; set; }
	public FileRenamer FileRenamer { get; set; }
	public QBittorrentClient QBittorrentClient { get; set; }
	public DownloadWatcher Downloadwatcher { get; set; }
	public MagazineFetcher(IOptions<MagazineFetcherOptions> configuration, RssFetcher rssFetcher,
	TorrentHistory torrentHistory, TorrentDownloader torrentDownloader,
	ArchiveExtractor archiveExtractor, MagazineClassifier magazineClassifier,
	FileRenamer fileRenamer, ILogger<MagazineFetcher> logger, QBittorrentClient qBittorrentClient,
	DownloadWatcher downloadWatcher) : this()
	{
		Configuration = configuration;
		RssFetcher = rssFetcher;
		TorrentHistory = torrentHistory;
		TorrentDownloader = torrentDownloader;
		ArchiveExtractor = archiveExtractor;
		MagazineClassifier = magazineClassifier;
		FileRenamer = fileRenamer;
		Logger = logger;
		QBittorrentClient = qBittorrentClient;
		Downloadwatcher = downloadWatcher;
	}

	internal async Task StartAsync(CancellationToken cancellationToken)
	{
		var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
		Logger.LogInformation($"Starting Pipeline in Profile '{environment}'");

		if (Configuration.Value.RunMode == RunMode.Once)
		{
			await RunOnceAsync(cancellationToken);
			return;
		}

		Logger.LogInformation("Starting in Daemon-Modus. Interval: " + $"{Configuration.Value.DaemonIntervalMinutes} Minutes");

		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				await RunOnceAsync(cancellationToken);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Fehler im Daemon-Durchlauf");
			}

			await Task.Delay(TimeSpan.FromMinutes(Configuration.Value.DaemonIntervalMinutes), cancellationToken);
		}

		Logger.LogInformation("Daemon wurde beendet.");
	}

	private async Task RunOnceAsync(CancellationToken cancellationToken)
	{
		var feedUrl = Configuration.Value.FeedUrl;
		var filter = Configuration.Value.RssFilter;

		// Get RSS
		var item = await RssFetcher.GetMatchingItemAsync(feedUrl, filter);
		if (item == null)
		{
			Logger.LogError("RSS doesn't have any entries.");
			return;
		}
		Logger.LogInformation($"Newest RSS-Entry: {item.Title}");

		// Check, if already processed
		if (TorrentHistory.AlreadyProcessed(item.Title))
		{
			Logger.LogInformation("Entry already processed. Exiting...");
			return;
		}

		// Download torrent
		Logger.LogInformation("Downloading Torrent...");
		var torrentBytes = await TorrentDownloader.DownloadTorrentAsync(item.Link);

		// Login to qBittorrent
		await QBittorrentClient.LoginAsync();

		// Send Torrent to qBittorrent and get Hash
		Logger.LogInformation("Sende Torrent an qBittorrent...");
		await QBittorrentClient.UploadTorrentAsync(torrentBytes);

		Logger.LogInformation("Waiting for finished Download...");

		// Wait until qBittorrent is finished
		Logger.LogInformation("Waiting for qBittorrent to finish...");
		var downloadDir = await Downloadwatcher.WaitForDownloadDirectoryAsync(
			Configuration.Value.QBittorrentClient.DownloadPath,
			item.Title,
			TimeSpan.FromHours(3));

		// Get place of the downloaded files
		var tempRoot = Configuration.Value.TempDirectory;
		var tempDir = Path.Combine(tempRoot, Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		// Extract Archive
		Logger.LogInformation("Extracting from {Download} to {TempDir}", downloadDir, tempDir);
		ArchiveExtractor.ExtractDirectory(downloadDir, tempDir);

		// Classify and rename files
		foreach (var file in Directory.GetFiles(tempDir, "*.pdf", SearchOption.AllDirectories))
		{
			var fileName = FileRenamer.NormalizeFileName(Path.GetFileName(file));
			var magazinePath = MagazineClassifier.Classify(fileName);
			if (magazinePath == null)
			{
				Logger.LogInformation($"Cant classify file: {fileName}");
				continue;
			}

			var year = FileRenamer.ExtractYear(fileName);
			var newName = FileRenamer.BuildNewName(
				Path.GetFileNameWithoutExtension(magazinePath),
				fileName
				);

			newName = FileRenamer.NormalizeFileName(newName);
			var targetDir = Path.Combine(magazinePath, year);
			Directory.CreateDirectory(targetDir);

			var targetPath = Path.Combine(targetDir, newName);

			Logger.LogInformation($"Moving file '{fileName}' to '{targetPath}'");
			File.Move(file, Path.Combine(targetDir, newName), overwrite: true);
		}

		// Update History
		TorrentHistory.MarkProcessed(item.Title);
		Logger.LogInformation("Saved Entry in History");

		// Cleanup
		try
		{
			Directory.Delete(tempDir, recursive: true);
			Logger.LogInformation("Temporary directory deleted: {TempDir}", tempDir);
		}
		catch (Exception ex)
		{
			Logger.LogWarning("Could not delete temporary directory {TempDir}: {Message}", tempDir, ex.Message);
		}

		Logger.LogInformation("Finished Pipeline");
	}
}
