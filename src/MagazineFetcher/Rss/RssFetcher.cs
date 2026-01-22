// <copyright file="RssFetcher.cs" company="Sascha Manns">
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

using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

using MagazineFetcher.AppConfig;

using Microsoft.Extensions.Logging;

namespace MagazineFetcher.Rss;

using System.Xml.Linq;

public class RssFetcher
{
	internal HttpClient _client = new();
	private readonly ILogger<RssFetcher> _logger;

	public RssFetcher(ILogger<RssFetcher> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

		// Vermeide Keep-Alive, um Probleme mit TLS‑Renegotiation / Verbindung-Reset zu reduzieren.
		_client.DefaultRequestHeaders.ConnectionClose = true;

		// Globalen User-Agent setzen (gilt für alle Requests über diesen HttpClient).
		// Passe die Zeichenfolge an (z.B. Version, Kontakt-URL).
		_client.DefaultRequestHeaders.UserAgent.ParseAdd("MagazineFetcher/1.0 (+https://github.com/saigkill/MagazineFetcher)");
	}

	internal async Task<RssItem?> GetMatchingItemAsync(string feedUrl, RssFilterConfig filter)
	{
		string xml = string.Empty;
		try
		{
			// Verwende HttpRequestMessage, um künftig feingranulare Einstellungen pro Request zu erlauben
			using var req = new HttpRequestMessage(HttpMethod.Get, feedUrl);
			// Ausdrücklich noch einmal: Connection: close (redundant zur Default-Header-Einstellung)
			req.Headers.ConnectionClose = true;

			using var resp = await _client.SendAsync(req, HttpCompletionOption.ResponseContentRead);
			resp.EnsureSuccessStatusCode();
			xml = await resp.Content.ReadAsStringAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching RSS feed from {FeedUrl}. Inner: {Inner}", feedUrl, ex.InnerException?.Message);

			if (ex.InnerException is SocketException se)
				_logger.LogDebug("SocketException ErrorCode: {Code}, SocketErrorCode: {SocketError}", (int)se.SocketErrorCode, se.SocketErrorCode);

			if (ex.InnerException is WebException we && we.Response is HttpWebResponse resp)
				_logger.LogDebug("HTTP response status: {Status}", resp.StatusCode);

			return null;
		}

		if (string.IsNullOrWhiteSpace(xml))
		{
			_logger.LogWarning("Received empty RSS feed from {FeedUrl}", feedUrl);
			return null;
		}

		XDocument doc;
		try
		{
			doc = XDocument.Parse(xml);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to parse RSS XML from {FeedUrl}", feedUrl);
			return null;
		}

		var items = doc.Descendants("item");

		foreach (var item in items)
		{
			var title = item.Element("title")?.Value ?? "";

			if (filter.TitleContains.Any(c => title.Contains(c, StringComparison.OrdinalIgnoreCase)))
				return ParseItem(item);

			if (!string.IsNullOrWhiteSpace(filter.TitleRegex) &&
				Regex.IsMatch(title, filter.TitleRegex))
				return ParseItem(item);
		}

		return null;
	}

	private RssItem ParseItem(XElement item)
	{
		return new RssItem(item.Element("title")?.Value ?? "", item.Element("link")?.Value ?? "",
			item.Element("pubDate")?.Value ?? "");
	}

	public record RssItem(string Title, string Link, string PubDate);
}
