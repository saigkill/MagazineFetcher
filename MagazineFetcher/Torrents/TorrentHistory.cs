// <copyright file="TorrentHistory.cs" company="Sascha Manns">
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

using System.ComponentModel;

using Microsoft.Extensions.Configuration;

namespace MagazineFetcher.Torrents;

public class TorrentHistory
{
	private readonly string _historyFile;

	public TorrentHistory(IConfiguration configuration)
	{
		var settings = configuration.Get<AppConfig.AppConfig>();
		_historyFile = settings.HistoryFile;
		if (!File.Exists(_historyFile))
			File.WriteAllText(_historyFile, "");
	}

	public bool AlreadyProcessed(string title)
	{
		return File.ReadAllLines(_historyFile).Contains(title);
	}

	public void MarkProcessed(string title)
	{
		File.AppendAllLines(_historyFile, new[] { title });
	}
}
