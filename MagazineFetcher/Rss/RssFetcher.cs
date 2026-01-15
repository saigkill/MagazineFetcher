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

using System.Text.RegularExpressions;

using MagazineFetcher.AppConfig;

using Microsoft.Extensions.Logging;

namespace MagazineFetcher.Rss;

using System.Xml.Linq;

public class RssFetcher
{
	private readonly HttpClient _client = new();

	public readonly ILogger<RssFetcher> _logger;

	public RssFetcher(ILogger<RssFetcher> logger)
	{
		_logger = logger;
	}

	public async Task<RssItem?> GetMatchingItemAsync(string feedUrl, RssFilterConfig filter)
	{
		var xml = await _client.GetStringAsync(feedUrl);
		var doc = XDocument.Parse(xml);
		var items = doc.Descendants("item");

		foreach (var item in items)
		{
			var title = item.Element("title")?.Value ?? "";

			if (filter.TitleContains.Any(c => title.Contains(c, StringComparison.OrdinalIgnoreCase)))
				return ParseItem(item);

			if (!string.IsNullOrWhiteSpace(filter.TitleRegex))
			{
				if (Regex.IsMatch(title, filter.TitleRegex))
					return ParseItem(item);
			}
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
