// <copyright file="ConfigValidator.cs" company="Sascha Manns">
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

using System.Text.RegularExpressions;

namespace MagazineFetcher.AppConfig;

public static class ConfigValidator
{
	public static void Validate(AppConfig config)
	{
		// Feed URL
		if (string.IsNullOrWhiteSpace(config.FeedUrl))
			throw new Exception("Missing FeedUrl in Config.");

		// History file
		if (string.IsNullOrWhiteSpace(config.HistoryFile))
			throw new Exception("Missing HistoryFile in Config.");

		// Temp Directory
		if (string.IsNullOrWhiteSpace(config.TempDirectory))
			throw new Exception("Missing TempDirectory");

		// Magazine mapping
		if (config.MagazineMapping == null || config.MagazineMapping.Count == 0)
			throw new Exception("MagazineMapping is emptyor missing.");

		// Rename pattern
		if (string.IsNullOrWhiteSpace(config.RenamePattern))
			throw new Exception("Missing RenamePattern.");

		// RSS Filter
		if (config.RssFilter == null)
			throw new Exception("Missing RssFilterin Config.");

		// Regex prüfen
		if (!string.IsNullOrWhiteSpace(config.RssFilter.TitleRegex))
		{
			try
			{
				_ = new Regex(config.RssFilter.TitleRegex);
			}
			catch (Exception ex)
			{
				throw new Exception($"Invalid TitleRegex: {ex.Message}");
			}
		}

		// qBittorrent
		if (config.QBittorrentClient == null)
			throw new Exception("Qbittorrent-Configuration missing.");

		if (string.IsNullOrWhiteSpace(config.QBittorrentClient.BaseUrl))
			throw new Exception("Qbittorrent.BaseUrl missing.");

		if (string.IsNullOrWhiteSpace(config.QBittorrentClient.Username))
			throw new Exception("Qbittorrent.Username missing.");

		if (string.IsNullOrWhiteSpace(config.QBittorrentClient.Password))
			throw new Exception("Qbittorrent.Password missing.");

		if (string.IsNullOrWhiteSpace(config.QBittorrentClient.DownloadPath))
			throw new Exception("QBittorrentClient.DownloadPath missing.");
	}
}

