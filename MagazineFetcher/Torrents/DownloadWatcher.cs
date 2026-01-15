// <copyright file="DownloadWatcher.cs" company="Sascha Manns">
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

using Microsoft.Extensions.Logging;

namespace MagazineFetcher.Torrents;

public class DownloadWatcher
{
	private readonly ILogger<DownloadWatcher> _logger;

	public DownloadWatcher(ILogger<DownloadWatcher> logger)
	{
		_logger = logger;
	}

	public async Task<string> WaitForNewDownloadAsync(string downloadPath, TimeSpan timeout)
	{
		var start = DateTime.UtcNow;
		var before = Directory.GetDirectories(downloadPath).ToHashSet();

		_logger.LogInformation("Watching download directory {Path}", downloadPath);

		while (DateTime.UtcNow - start < timeout)
		{
			var after = Directory.GetDirectories(downloadPath).ToHashSet();
			var newDirs = after.Except(before).ToList();

			if (newDirs.Any())
			{
				var dir = newDirs.First();
				_logger.LogInformation("New download directory detected: {Dir}", dir);

				await WaitForStableDirectory(dir);
				return dir;
			}

			await Task.Delay(2000);
		}

		throw new TimeoutException("No new download directory detected in time.");
	}

	private async Task WaitForStableDirectory(string dir)
	{
		_logger.LogInformation("Waiting for directory to become stable: {Dir}", dir);

		long lastSize = -1;

		while (true)
		{
			long size = Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
				.Sum(f => new FileInfo(f).Length);

			if (size == lastSize)
			{
				_logger.LogInformation("Directory is stable: {Dir}", dir);
				return;
			}

			lastSize = size;
			await Task.Delay(3000);
		}
	}
}

