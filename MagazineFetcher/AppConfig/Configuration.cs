// <copyright file="Configuration.cs" company="Sascha Manns">
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

using System.ComponentModel.DataAnnotations;

namespace MagazineFetcher.AppConfig;

public class Configuration
{
	[Required] public string FeedUrl { get; set; } = "";
	[Required] public string WatchDir { get; set; } = "";
	[Required] public string HistoryFile { get; set; } = "";
	[Required] public string TempDirectory { get; set; } = "";
	[Required] public string RenamePattern { get; set; } = "{Magazine}-{Issue}-{Year}.pdf";
	[Required] public Dictionary<string, string> MagazineMapping { get; set; } = new();
	public QBittorrentClientConfig QBittorrentClient { get; set; } = new();
	public RssFilterConfig RssFilter { get; set; } = new();
}

public class QBittorrentClientConfig
{
	[Required] public string BaseUrl { get; set; } = string.Empty;
	[Required] public string Username { get; set; } = string.Empty;
	[Required] public string Password { get; set; } = string.Empty;
	[Required] public string DownloadPath { get; set; } = string.Empty;
}

public class RssFilterConfig
{
	[Required] public List<string> TitleContains { get; set; } = new();
	public string? TitleRegex { get; set; }
}
