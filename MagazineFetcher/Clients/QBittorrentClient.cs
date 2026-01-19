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

using MagazineFetcher.AppConfig;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MagazineFetcher.Clients;

public class QBittorrentClient
{
	private readonly HttpClient _client;
	private readonly ILogger<QBittorrentClient> _logger;

	public QBittorrentClient(IOptions<Configuration> configuration, ILogger<QBittorrentClient> logger)
	{
		_logger = logger;
		var cfg = configuration.Value.QBittorrentClient;

		_client = new HttpClient { BaseAddress = new Uri(cfg.BaseUrl) };

		var login = new FormUrlEncodedContent(new[]
		{
			new KeyValuePair<string,string>("username", cfg.Username),
			new KeyValuePair<string,string>("password", cfg.Password)
		});

		var response = _client.PostAsync("/api/v2/auth/login", login).Result;
		if (!response.IsSuccessStatusCode)
		{
			var body = response.Content.ReadAsStringAsync().Result;
			_logger.LogError("Login failed: {Status} - {Body}", response.StatusCode, body);
			throw new Exception("Login with qBittorrent failed.");
		}

		_logger.LogInformation("QBittorrentClient initialized");
	}

	public async Task UploadTorrentAsync(byte[] torrentBytes)
	{
		_logger.LogInformation("Uploading torrent to qBittorrent…");

		var content = new MultipartFormDataContent
		{
			{ new ByteArrayContent(torrentBytes), "torrents", "file.torrent" }
		};

		var response = await _client.PostAsync("/api/v2/torrents/add", content);
		if (!response.IsSuccessStatusCode)
			throw new Exception("Error uploading torrent.");
	}
}
