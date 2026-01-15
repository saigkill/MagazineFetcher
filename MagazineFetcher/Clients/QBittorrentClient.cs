// <copyright file="QBittorrentClient.cs" company="Sascha Manns">
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

using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MagazineFetcher.Clients;

public class QBittorrentClient
{
	private readonly HttpClient _client;
	private ILogger<QBittorrentClient> _logger;

	public QBittorrentClient(IConfiguration configuration, ILogger<QBittorrentClient> logger)
	{
		_logger = logger;
		var settings = configuration.Get<AppConfig.AppConfig>();
		var baseUrl = settings.QBittorrentClient.BaseUrl;
		var username = settings.QBittorrentClient.Username;
		var password = settings.QBittorrentClient.Password;
		_logger.LogInformation(baseUrl);
		_client = new HttpClient { BaseAddress = new Uri(baseUrl) };

		var login = new FormUrlEncodedContent(new[]
		{
			new KeyValuePair<string,string>("username", username),
			new KeyValuePair<string,string>("password", password)
		});

		var response = _client.PostAsync("/api/v2/auth/login", login).Result;
		if (!response.IsSuccessStatusCode)
			throw new Exception("Login with qBittorrent failed.");
		_logger.LogInformation("QBittorrentClient done");
	}

	// ------------------------------------------------------------
	// 1. Get all Torrents
	// ------------------------------------------------------------
	public async Task<List<QbTorrentInfo>> GetAllTorrentsAsync()
	{
		var json = await _client.GetStringAsync("/api/v2/torrents/info");
		return JsonSerializer.Deserialize<List<QbTorrentInfo>>(json) ?? new();
	}

	// ------------------------------------------------------------
	// 2. Get Torrent per hash
	// ------------------------------------------------------------
	public async Task<QbTorrentInfo?> GetTorrentByHashAsync(string hash)
	{
		var torrents = await GetAllTorrentsAsync();
		return torrents.FirstOrDefault(t =>
			t.Hash.Equals(hash, StringComparison.OrdinalIgnoreCase));
	}

	// ------------------------------------------------------------
	// 3. Upload Torrent and get hash
	// ------------------------------------------------------------
	public async Task<string> AddTorrentAndGetHashAsync(byte[] torrentBytes, string expectedTitle)
	{
		// List before
		var before = await GetAllTorrentsAsync();
		var beforeHashes = before.Select(t => t.Hash).ToHashSet();

		_logger.LogInformation("Uploading torrent to qBittorrent…");

		// Upload
		var content = new MultipartFormDataContent
		{
			{ new ByteArrayContent(torrentBytes), "torrents", "file.torrent" }
		};

		var response = await _client.PostAsync("/api/v2/torrents/add", content);
		if (!response.IsSuccessStatusCode)
			throw new Exception("Fehler beim Hochladen des Torrents.");

		// Wait until qBittorrent registered the Torrent
		for (int i = 0; i < 200; i++) // 100 Sekunden
		{
			await Task.Delay(500);

			var after = await GetAllTorrentsAsync();

			// 1. Try to find a new hash
			var newTorrent = after.FirstOrDefault(t => !beforeHashes.Contains(t.Hash));
			if (newTorrent != null && !string.IsNullOrWhiteSpace(newTorrent.Hash))
			{
				_logger.LogInformation("Torrent registered with {Hash}", newTorrent.Hash);
				return newTorrent.Hash;
			}

			// 2. Fallback: match by title (NAS often reorders list)
			var byTitle = after.FirstOrDefault(t =>
				!string.IsNullOrWhiteSpace(t.Name) &&
				t.Name.ToLowerInvariant().Contains(expectedTitle, StringComparison.OrdinalIgnoreCase));

			if (byTitle != null && !string.IsNullOrWhiteSpace(byTitle.Hash))
			{
				_logger.LogWarning("Fallback title match used. Hash: {Hash}", byTitle.Hash);
				return byTitle.Hash;
			}

			// 3. Fallback: new Torrent without hash yet
			var newUnknown = after.FirstOrDefault(t =>
				!beforeHashes.Contains(t.Hash) ||
				string.IsNullOrWhiteSpace(t.Hash) && t.State is "metaDL" or "downloading");

			if (newUnknown != null)
			{
				_logger.LogWarning("Torrent detected but hash not available. Waiting...");
				continue;
			}
		}

		throw new Exception("Torrent not registered.");
	}

	// ------------------------------------------------------------
	// 4. Wait until Torrent is finished
	// ------------------------------------------------------------
	public async Task WaitForCompletionAsync(string hash, TimeSpan timeout)
	{
		var start = DateTime.UtcNow;

		while (true)
		{
			if (DateTime.UtcNow - start > timeout)
				throw new TimeoutException("Download hat das Zeitlimit überschritten.");

			var info = await GetTorrentByHashAsync(hash);

			if (info == null)
			{
				_logger.LogInformation("Torrent noch nicht in der Liste.");
				await Task.Delay(2000);
				continue;
			}

			_logger.LogInformation($"Status: {info.State}");

			if (IsErrorState(info.State))
				throw new Exception($"Torrentfehler: {info.State}");

			if (IsCompletedState(info.State))
			{
				_logger.LogInformation("Torrent download finished.");
				return;
			}

			await Task.Delay(3000);
		}
	}

	private bool IsCompletedState(string state)
	{
		return state is "uploading" or "stalledUP" or "pausedUP" or "queuedUP" or "checkingUP";
	}

	private bool IsErrorState(string state)
	{
		return state is "error" or "missingFiles" or "unknown";
	}
}

// ------------------------------------------------------------
// Datenmodell für qBittorrent
// ------------------------------------------------------------
public class QbTorrentInfo
{
	public string Name { get; set; }
	public string Hash { get; set; }
	public string State { get; set; }
	public string SavePath { get; set; }
}
