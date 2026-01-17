// <copyright file="ArchiveExtractor.cs" company="Sascha Manns">
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

namespace MagazineFetcher.Extraction;

using SharpCompress.Archives;
using SharpCompress.Common;

public class ArchiveExtractor
{
	public readonly ILogger<ArchiveExtractor> _logger;
	public ArchiveExtractor(ILogger<ArchiveExtractor> logger)
	{
		_logger = logger;
	}

	public void ExtractDirectory(string sourceDir, string outputDir)
	{
		var archives = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories)
			.Where(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
						f.EndsWith(".rar", StringComparison.OrdinalIgnoreCase) ||
						f.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
			.ToList();

		if (!archives.Any())
		{
			_logger.LogInformation("No archives found. Copying files directly…");

			foreach (var file in Directory.GetFiles(sourceDir, "*.pdf", SearchOption.AllDirectories))
			{
				var dest = Path.Combine(outputDir, Path.GetFileName(file));
				File.Copy(file, dest, overwrite: true);
			}

			return;
		}

		foreach (var archive in archives)
		{
			_logger.LogInformation("Extracting archive {Archive}", archive);
			Extract(archive, outputDir);
		}
	}

	public void Extract(string archivePath, string outputDir)
	{
		try
		{
			using var archive = ArchiveFactory.Open(archivePath);

			foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
			{
				entry.WriteToDirectory(outputDir, new ExtractionOptions
				{
					ExtractFullPath = true,
					Overwrite = true
				});
			}
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error while extracting the archive: {ex.Message}", ex);
			throw;
		}
	}
}
