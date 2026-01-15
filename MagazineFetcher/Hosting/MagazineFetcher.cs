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

using NLog;

using QBittorrentClient = MagazineFetcher.Clients.QBittorrentClient;

namespace MagazineFetcher.Hosting;

public class MagazineFetcher()
{
	public IConfiguration _configuration { get; set; }
	public ILogger<MagazineFetcher> _logger { get; set; }
	public RssFetcher _rssFetcher { get; set; }
	public TorrentHistory _torrentHistory { get; set; }
	public TorrentDownloader _torrentDownloader { get; set; }
	public ArchiveExtractor _archiveExtractor { get; set; }
	public MagazineClassifier _magazineClassifier { get; set; }
	public FileRenamer _fileRenamer { get; set; }
	public QBittorrentClient _qBittorrentClient { get; set; }
	public MagazineFetcher(IConfiguration configuration, RssFetcher rssFetcher,
	TorrentHistory torrentHistory, TorrentDownloader torrentDownloader,
	ArchiveExtractor archiveExtractor, MagazineClassifier magazineClassifier,
	FileRenamer fileRenamer, ILogger<MagazineFetcher> logger, QBittorrentClient qBittorrentClient) : this()
	{
		_configuration = configuration;
		_rssFetcher = rssFetcher;
		_torrentHistory = torrentHistory;
		_torrentDownloader = torrentDownloader;
		_archiveExtractor = archiveExtractor;
		_magazineClassifier = magazineClassifier;
		_fileRenamer = fileRenamer;
		_logger = logger;
		_qBittorrentClient = qBittorrentClient;
	}

	public async Task StartAsync()
	{
		var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
		_logger.LogInformation($"Starte Pipeline im Profil '{environment}'");

		var settings = _configuration.Get<AppConfig.AppConfig>();
		ConfigValidator.Validate(settings);
		var feedUrl = settings.FeedUrl;
		var filter = settings.RssFilter;

		// Get RSS
		var latest = await _rssFetcher.GetMatchingItemAsync(feedUrl, filter);
		if (latest == null)
		{
			_logger.LogError("RSS doesn't have any entries.");
			return;
		}
		_logger.LogInformation($"Newest RSS-Entry: {latest.Title}");

		// Check, if already processed
		if (_torrentHistory.AlreadyProcessed(latest.Title))
		{
			_logger.LogInformation("Entry already processed. Exiting...");
			return;
		}

		// Download torrent
		_logger.LogInformation("Downloading Torrent...");
		var torrentBytes = await _torrentDownloader.DownloadTorrentAsync(latest.Link);

		// Send Torrent to qBittorrent and get Hash
		_logger.LogInformation("Sende Torrent an qBittorrent...");
		var hash = await _qBittorrentClient.AddTorrentAndGetHashAsync(torrentBytes, latest.Title);

		_logger.LogInformation($"Torrent-Hash: {hash}");
		_logger.LogInformation("Waiting for finished Download...");

		// Wait until qBittorrent is finished
		_logger.LogInformation("Waiting for qBittorrent to finish...");
		await _qBittorrentClient.WaitForCompletionAsync(hash, TimeSpan.FromHours(2));

		// Get place of the downloaded files
		var torrentInfo = await _qBittorrentClient.GetTorrentByHashAsync(latest.Title);
		if (torrentInfo == null)
		{
			_logger.LogError("Torrent not found in QBittorrent");
			return;
		}

		var downloadDir = torrentInfo.SavePath;
		_logger.LogInformation($"Download finished. Saved in: {downloadDir}");

		// Extract Archive
		var tempDir = Path.Combine("/tmp/magazines", Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		_logger.LogInformation($"Extracting archive to: {tempDir} ...");
		_archiveExtractor.Extract(downloadDir, tempDir);

		// Classify and rename files
		foreach (var file in Directory.GetFiles(tempDir, "*.pdf", SearchOption.AllDirectories))
		{
			var fileName = Path.GetFileName(file);
			var magazinePath = _magazineClassifier.Classify(fileName);
			if (magazinePath == null)
			{
				_logger.LogInformation($"Cant classify file: {fileName}");
			}

			var year = _fileRenamer.ExtractYear(fileName);
			var newName = _fileRenamer.BuildNewName(
				Path.GetFileNameWithoutExtension(magazinePath),
				fileName
				);

			var targetDir = Path.Combine(magazinePath, year);
			Directory.CreateDirectory(targetDir);

			var targetPath = Path.Combine(targetDir, newName);

			_logger.LogInformation($"Moving file '{fileName}' to '{targetPath}'");
			File.Move(file, Path.Combine(targetDir, newName), overwrite: true);
		}

		// Update History
		_torrentHistory.MarkProcessed(latest.Title);
		_logger.LogInformation("Saved Entry in History");

		_logger.LogInformation("Finished Pipeline");
	}


}
