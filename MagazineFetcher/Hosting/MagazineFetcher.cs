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
	public IOptions<Configuration> _configuration { get; set; }
	public ILogger<MagazineFetcher> _logger { get; set; }
	public RssFetcher _rssFetcher { get; set; }
	public TorrentHistory _torrentHistory { get; set; }
	public TorrentDownloader _torrentDownloader { get; set; }
	public ArchiveExtractor _archiveExtractor { get; set; }
	public MagazineClassifier _magazineClassifier { get; set; }
	public FileRenamer _fileRenamer { get; set; }
	public QBittorrentClient _qBittorrentClient { get; set; }
	public DownloadWatcher _downloadwatcher { get; set; }
	public MagazineFetcher(IOptions<Configuration> configuration, RssFetcher rssFetcher,
	TorrentHistory torrentHistory, TorrentDownloader torrentDownloader,
	ArchiveExtractor archiveExtractor, MagazineClassifier magazineClassifier,
	FileRenamer fileRenamer, ILogger<MagazineFetcher> logger, QBittorrentClient qBittorrentClient,
	DownloadWatcher downloadWatcher) : this()
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
		_downloadwatcher = downloadWatcher;
	}

	public async Task StartAsync()
	{
		var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
		_logger.LogInformation($"Starte Pipeline im Profil '{environment}'");


		var feedUrl = _configuration.Value.FeedUrl;
		var filter = _configuration.Value.RssFilter;

		// Get RSS
		var item = await _rssFetcher.GetMatchingItemAsync(feedUrl, filter);
		if (item == null)
		{
			_logger.LogError("RSS doesn't have any entries.");
			return;
		}
		_logger.LogInformation($"Newest RSS-Entry: {item.Title}");

		// Check, if already processed
		if (_torrentHistory.AlreadyProcessed(item.Title))
		{
			_logger.LogInformation("Entry already processed. Exiting...");
			return;
		}

		// Download torrent
		_logger.LogInformation("Downloading Torrent...");
		var torrentBytes = await _torrentDownloader.DownloadTorrentAsync(item.Link);

		// Send Torrent to qBittorrent and get Hash
		_logger.LogInformation("Sende Torrent an qBittorrent...");
		await _qBittorrentClient.UploadTorrentAsync(torrentBytes);

		_logger.LogInformation("Waiting for finished Download...");

		// Wait until qBittorrent is finished
		_logger.LogInformation("Waiting for qBittorrent to finish...");
		var downloadDir = await _downloadwatcher.WaitForDownloadDirectoryAsync(
			_configuration.Value.QBittorrentClient.DownloadPath,
			item.Title,
			TimeSpan.FromHours(3));

		// Get place of the downloaded files
		var tempRoot = _configuration.Value.TempDirectory;
		var tempDir = Path.Combine(tempRoot, Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		// Extract Archive
		_logger.LogInformation("Extracting from {Download} to {TempDir}", downloadDir, tempDir);
		_archiveExtractor.ExtractDirectory(downloadDir, tempDir);

		// Classify and rename files
		foreach (var file in Directory.GetFiles(tempDir, "*.pdf", SearchOption.AllDirectories))
		{
			var fileName = _fileRenamer.NormalizeFileName(Path.GetFileName(file));
			var magazinePath = _magazineClassifier.Classify(fileName);
			if (magazinePath == null)
			{
				_logger.LogInformation($"Cant classify file: {fileName}");
				continue;
			}

			var year = _fileRenamer.ExtractYear(fileName);
			var newName = _fileRenamer.BuildNewName(
				Path.GetFileNameWithoutExtension(magazinePath),
				fileName
				);

			newName = _fileRenamer.NormalizeFileName(newName);
			var targetDir = Path.Combine(magazinePath, year);
			Directory.CreateDirectory(targetDir);

			var targetPath = Path.Combine(targetDir, newName);

			_logger.LogInformation($"Moving file '{fileName}' to '{targetPath}'");
			File.Move(file, Path.Combine(targetDir, newName), overwrite: true);
		}

		// Update History
		_torrentHistory.MarkProcessed(item.Title);
		_logger.LogInformation("Saved Entry in History");

		// Cleanup
		try
		{
			Directory.Delete(tempDir, recursive: true);
			_logger.LogInformation("Temporary directory deleted: {TempDir}", tempDir);
		}
		catch (Exception ex)
		{
			_logger.LogWarning("Could not delete temporary directory {TempDir}: {Message}", tempDir, ex.Message);
		}

		_logger.LogInformation("Finished Pipeline");
	}
}
